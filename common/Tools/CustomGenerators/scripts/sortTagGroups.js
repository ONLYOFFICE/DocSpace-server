const fs = require('fs');
const path = require('path');

const inputFilePath = path.join(__dirname, '../json/api-docs.json');
const outputFilePath = path.join(__dirname, '../json/api-docs.json');

fs.readFile(inputFilePath, 'utf8', (err, data) => {
  if (err) {
    console.error('Cannot read file:', err);
    return;
  }

  let jsonData;
  try {
    jsonData = JSON.parse(data);
  } catch (parseErr) {
    console.error('Cannot parse json:', parseErr);
    return;
  }

  const tags = jsonData.tags || [];
  const groups = {};

  for (const tag of tags) {
    const tagName = tag.name;
    const [groupName, subTag] = tagName.split(' / ');

    if (!groups[groupName]) {
      groups[groupName] = new Set();
    }

    groups[groupName].add(tagName);
  }

  const xTagGroups = Object.entries(groups)
    .map(([name, tagsSet]) => ({
      name,
      tags: Array.from(tagsSet).sort((a, b) => a.localeCompare(b))
    }))
    .sort((a, b) => a.name.localeCompare(b.name)); 

  jsonData['x-tagGroups'] = xTagGroups;

  fs.writeFile(outputFilePath, JSON.stringify(jsonData, null, 2), 'utf8', (writeErr) => {
    if (writeErr) {
      console.error('Cannot write file:', writeErr);
    } else {
      console.log('Succesfully saved', outputFilePath);
    }
  });
});