const fs = require('fs');
const path = require('path');

const inputFilePath = path.join(__dirname, '../json/api-docs.json');
const outputFilePath = path.join(__dirname, '../json/api-docs.json');

function processNode(node, preferredEnumType = null, parentNode = null) {
  if (typeof node !== 'object' || node === null) return;

  for (const key in node) {
    const value = node[key];

    if (key === 'summary' && typeof node[key] === 'string') node[key] = value.replace(/"/g, "");
    if (key === 'description' && typeof node[key] === 'string') node[key] = value.replace(/"/g, "");

    if (value && typeof value === 'object') {
      if ((key === 'anyOf' || key === 'oneOf') && Array.isArray(value) && isEnumAnyOf(value)) {
        let targetType = preferredEnumType || node['x-enum-type'] || 'integer';

        const preferred = value.find(item => (item['x-enum-type'] || item.type) === targetType);
        if (preferred) {
          const {
            enum: enumValues,
            example,
            description: originalDescription,
            ['x-enum-varnames']: enumVarnames,
            ['x-enum-descriptions']: enumDescriptions
          } = preferred;

          node.enum = enumValues;
          node.type = targetType;
          if (example !== undefined) node.example = example;
          if (enumVarnames) node['x-enum-varnames'] = enumVarnames;
          if (enumDescriptions) node['x-enum-descriptions'] = enumDescriptions;
          node.description = parentNode?.description || originalDescription;

          delete node[key];
        }
      } else {
        processNode(value, preferredEnumType, node);
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

function isEnumAnyOf(arr) {
  return arr.every(item =>
    item && typeof item === 'object' && !item.$ref && (item.type === 'string' || item.type === 'integer') && Array.isArray(item.enum)
  );
}
