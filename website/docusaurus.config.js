/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'BinaryParsec',
  tagline: 'Binary tokenization and parsing for F#',
  favicon: 'img/logo.svg',
  url: 'http://localhost:3000',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  organizationName: 'openai',
  projectName: 'BinaryParsec',
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          path: '../docs',
          routeBasePath: 'docs',
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: undefined,
          includeCurrentVersion: false,
          versions: {
            '0.1.0': {
              label: '0.1.0',
            },
          },
          showLastUpdateAuthor: false,
          showLastUpdateTime: false,
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      }),
    ],
  ],
  themeConfig: {
    navbar: {
      title: 'BinaryParsec',
      logo: {
        alt: 'BinaryParsec logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'docsVersionDropdown',
          position: 'left',
        },
        {
          href: '/docs/',
          label: 'Guide',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            { label: 'Start', to: '/docs/' },
            { label: 'Integrate', to: '/docs/integrate/how-to' },
            { label: 'Model', to: '/docs/model' },
          ],
        },
      ],
      copyright: `Copyright ${new Date().getFullYear()} BinaryParsec`,
    },
    prism: {
      additionalLanguages: ['fsharp'],
    },
  },
};

module.exports = config;
