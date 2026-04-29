/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docsSidebar: [
    {
      type: 'category',
      label: 'Start',
      link: {
        type: 'doc',
        id: 'README',
      },
      items: [
        'tutorials/README',
        'tutorials/parse-your-first-sized-message',
      ],
    },
    {
      type: 'category',
      label: 'Integrate',
      link: {
        type: 'doc',
        id: 'how-to/README',
      },
      items: [
        'how-to/README',
        'integrations/README',
        'integrations/csharp',
        'integrations/fsharp',
        'integrations/testing',
      ],
    },
    {
      type: 'category',
      label: 'Understand',
      link: {
        type: 'doc',
        id: 'explanation/README',
      },
      items: [
        'explanation/README',
        'explanation/ARCHITECTURE',
        'explanation/backend-seam',
        'explanation/contiguous-input-model',
        'explanation/snippet-milestones-and-core-coverage',
        'explanation/EXAMPLE-CORPUS',
        'explanation/CSHARP_INTEROP',
        'explanation/modbus-package-shape',
        'explanation/png-package-shape',
        'explanation/can-package-shape',
        'explanation/deflate-package-shape',
        'explanation/elf-package-shape',
        'explanation/midi-package-shape',
        'explanation/protobuf-package-shape',
      ],
    },
    {
      type: 'category',
      label: 'Model',
      link: {
        type: 'doc',
        id: 'reference/README',
      },
      items: [
        'reference/README',
        'reference/api/README',
        'reference/core-reading-patterns',
        'reference/modbus-package',
        'reference/modbus-authoritative-sources',
        'reference/can-package',
        'reference/can-authoritative-sources',
        'reference/deflate-package',
        'reference/deflate-authoritative-sources',
        'reference/elf-package',
        'reference/elf-authoritative-sources',
        'reference/midi-package',
        'reference/midi-authoritative-sources',
        'reference/png-package',
        'reference/png-authoritative-sources',
        'reference/protobuf-package',
        'reference/protobuf-authoritative-sources',
      ],
    },
    {
      type: 'category',
      label: 'Examples',
      link: {
        type: 'doc',
        id: 'examples/README',
      },
      items: [
        'examples/README',
        'examples/core-sized-message',
        'examples/can-classic-frame',
        'examples/png-initial-slice',
        'examples/protobuf-wire-message',
        'examples/modbus-rtu-frame',
        'examples/elf-program-header',
        'examples/midi-running-status',
      ],
    },
    {
      type: 'category',
      label: 'Measure',
      link: {
        type: 'doc',
        id: 'measure/README',
      },
      items: [
        'measure/README',
        'measure/validation',
      ],
    },
  ],
};

module.exports = sidebars;
