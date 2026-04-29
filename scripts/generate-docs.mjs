import { cp, mkdir, readFile, readdir, rm, writeFile, copyFile } from 'node:fs/promises';
import { dirname, join, resolve, basename } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { spawnSync } from 'node:child_process';

const scriptDir = dirname(fileURLToPath(import.meta.url));
const rootDir = resolve(scriptDir, '..');
const docsDir = join(rootDir, 'docs');
const examplesDir = join(rootDir, 'examples');
const docsVersion = '0.1.0';
const websiteDir = join(rootDir, 'website');
const websiteSourceDir = join(rootDir, 'website', 'static', 'source');
const versionedDocsDir = join(websiteDir, 'versioned_docs', `version-${docsVersion}`);
const versionedSidebarsPath = join(websiteDir, 'versioned_sidebars', `version-${docsVersion}-sidebars.json`);

const examples = [
  { page: 'examples/core-sized-message.md', script: 'core-sized-message.fsx' },
  { page: 'examples/can-classic-frame.md', script: 'can-classic-frame.fsx' },
  { page: 'examples/png-initial-slice.md', script: 'png-initial-slice.fsx' },
  { page: 'examples/protobuf-wire-message.md', script: 'protobuf-wire-message.fsx' },
  { page: 'examples/modbus-rtu-frame.md', script: 'modbus-rtu-frame.fsx' },
  { page: 'examples/elf-program-header.md', script: 'elf-program-header.fsx' },
  { page: 'examples/midi-running-status.md', script: 'midi-running-status.fsx' },
];

const sourceCopies = [
  'src/BinaryParsec/BinaryParsec.fs',
  'src/BinaryParsec.Protocols.Can/CanClassic.fs',
  'src/BinaryParsec.Protocols.Can/CanClassicParser.fs',
  'src/BinaryParsec.Protocols.Can/CanClassicMaterializer.fs',
  'src/BinaryParsec.Protocols.Deflate/Deflate.fs',
  'src/BinaryParsec.Protocols.Deflate/DeflateParser.fs',
  'src/BinaryParsec.Protocols.Deflate/DeflateDynamicPrelude.fs',
  'src/BinaryParsec.Protocols.Elf/Elf.fs',
  'src/BinaryParsec.Protocols.Elf/ElfParser.fs',
  'src/BinaryParsec.Protocols.Elf/ElfFileHeader.fs',
  'src/BinaryParsec.Protocols.Midi/Midi.fs',
  'src/BinaryParsec.Protocols.Midi/MidiChannelParser.fs',
  'src/BinaryParsec.Protocols.Midi/MidiChannelMaterializer.fs',
  'src/BinaryParsec.Protocols.Modbus/ModbusRtu.fs',
  'src/BinaryParsec.Protocols.Modbus/ModbusTcp.fs',
  'src/BinaryParsec.Protocols.Modbus/ModbusPduParser.fs',
  'src/BinaryParsec.Protocols.Png/Png.fs',
  'src/BinaryParsec.Protocols.Png/PngParser.fs',
  'src/BinaryParsec.Protocols.Png/PngMaterializer.fs',
  'src/BinaryParsec.Protocols.Protobuf/ProtobufWire.fs',
  'src/BinaryParsec.Protocols.Protobuf/ProtobufWireParser.fs',
  'src/BinaryParsec.Protocols.Protobuf/ProtobufWireMaterializer.fs',
  'tests/BinaryParsec.Tests/BinaryParsec.Tests.fsproj',
  'tests/BinaryParsec.Tests/ZeroAllocationHotPathTests.fs',
];

async function main() {
  await syncSources();
  await syncExamples();
  await refreshExamples();
  await syncVersionedDocs();
}

async function syncSources() {
  for (const relative of sourceCopies) {
    const from = join(rootDir, relative);
    const to = join(websiteSourceDir, relative);
    await mkdir(dirname(to), { recursive: true });
    await copyFile(from, to);
  }
}

async function syncExamples() {
  const outDir = join(websiteSourceDir, 'examples');
  await mkdir(outDir, { recursive: true });

  const files = await readdir(examplesDir);
  for (const file of files) {
    if (file.endsWith('.fsx')) {
      await copyFile(join(examplesDir, file), join(outDir, file));
    }
  }
}

async function refreshExamples() {
  for (const example of examples) {
    const pagePath = join(docsDir, example.page);
    const scriptPath = join(examplesDir, example.script);
    const output = runScript(scriptPath);
    const updated = replaceObservedOutput(await readFile(pagePath, 'utf8'), output);
    await writeFile(pagePath, updated, 'utf8');
  }
}

function runScript(scriptPath) {
  const result = spawnSync('dotnet', ['fsi', '--quiet', scriptPath], {
    cwd: rootDir,
    encoding: 'utf8',
  });

  if (result.status !== 0) {
    throw new Error(`Failed to run ${basename(scriptPath)}:\n${result.stdout}\n${result.stderr}`);
  }

  return result.stdout.trimEnd();
}

function replaceObservedOutput(page, output) {
  const marker = 'Observed output:\n\n';
  const sourceMarker = '\n\nSource:';
  const start = page.indexOf(marker);
  const sourceIndex = page.indexOf(sourceMarker, start + marker.length);

  if (start === -1 || sourceIndex === -1) {
    throw new Error('Could not update observed output block.');
  }

  const prefix = page.slice(0, start + marker.length);
  const suffix = page.slice(sourceIndex);
  return `${prefix}\`\`\`text\n${output}\n\`\`\`${suffix}`;
}

async function syncVersionedDocs() {
  await rm(versionedDocsDir, { recursive: true, force: true });
  await mkdir(dirname(versionedDocsDir), { recursive: true });
  await cp(docsDir, versionedDocsDir, { recursive: true });

  const sidebarsModule = await import(pathToFileURL(join(websiteDir, 'sidebars.js')).href);
  await mkdir(dirname(versionedSidebarsPath), { recursive: true });
  await writeFile(
    versionedSidebarsPath,
    `${JSON.stringify(sidebarsModule.default, null, 2)}\n`,
    'utf8',
  );
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
