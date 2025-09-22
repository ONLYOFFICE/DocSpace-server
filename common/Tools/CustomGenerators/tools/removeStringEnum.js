const fs = require('fs');
const path = require('path');

const inputFilePath = path.join(__dirname, 'api-docs.json');
const outputFilePath = path.join(__dirname, 'api-docs.json');

function processNode(node, parentNode = null) {
  if (typeof node !== 'object' || node === null) {
    return;
  }

  for (const key in node) {

    if (key === 'summary' && typeof node[key] === 'string') {
      node[key] = node[key].replace(/"/g, "");
    }
    if (key === 'description' && typeof node[key] === 'string') {
      node[key] = node[key].replace(/"/g, "");
    }
    if (node[key] && typeof node[key] === 'object') {
      if ((key === 'anyOf' || key === 'oneOf') && Array.isArray(node[key])) {
        const integerEnum = node[key].find(item => item.type === 'integer' && Array.isArray(item.enum));
        if (integerEnum) {
          const {
            enum: enumValues,
            example,
            description: originalDescription,
            ['x-enum-varnames']: enumVarnames,
            ['x-enum-descriptions']: enumDescriptions
          } = integerEnum;

          node.enum = enumValues;
          node.type = 'integer';
          if (example !== undefined) node.example = example;

          if (enumVarnames) node['x-enum-varnames'] = enumVarnames;
          if (enumDescriptions) node['x-enum-descriptions'] = enumDescriptions;

          node.description = parentNode?.description || originalDescription;

          delete node[key];
        }
      } else {
        processNode(node[key], node);
      }
    }
  }
}

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

  processNode(jsonData);

  fs.writeFile(outputFilePath, JSON.stringify(jsonData, null, 2), 'utf8', (writeErr) => {
    if (writeErr) {
      console.error('Cannot write file:', writeErr);
    } else {
      console.log('Succesfully saved', outputFilePath);
    }
  });
});
