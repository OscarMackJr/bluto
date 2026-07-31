#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import Ajv2020 from 'ajv/dist/2020.js';
import addFormats from 'ajv-formats';

const args = process.argv.slice(2);
if (args[0] !== 'validate') {
  console.error('Usage: ajv validate --spec=draft2020 -s <schema.json> -d <data.json>');
  process.exit(2);
}

function optionValue(shortName, longName) {
  const index = args.findIndex((arg) => arg === shortName || arg === longName || arg.startsWith(`${longName}=`));
  if (index === -1) {
    return undefined;
  }

  const arg = args[index];
  if (arg.includes('=')) {
    return arg.slice(arg.indexOf('=') + 1);
  }

  return args[index + 1];
}

const schemaPath = optionValue('-s', '--schema');
const dataPath = optionValue('-d', '--data');
if (!schemaPath || !dataPath) {
  console.error('Both -s/--schema and -d/--data are required.');
  process.exit(2);
}

const schema = JSON.parse(fs.readFileSync(path.resolve(schemaPath), 'utf8'));
const data = JSON.parse(fs.readFileSync(path.resolve(dataPath), 'utf8'));
const ajv = new Ajv2020({ allErrors: true, strict: true });
addFormats(ajv);
const validate = ajv.compile(schema);
const valid = validate(data);
if (!valid) {
  console.error(JSON.stringify(validate.errors, null, 2));
  process.exit(1);
}

console.log(`${dataPath} valid`);
