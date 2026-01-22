import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'UnionGenerator',
  tagline: 'Powerful Discriminated Unions for C# with Source Generators',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Set the production url of your site here
  url: 'https://selcukgural.github.io',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/UnionGenerator/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'selcukgural', // Usually your GitHub org/user name.
  projectName: 'UnionGenerator', // Usually your repo name.

  onBrokenLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl:
            'https://github.com/selcukgural/UnionGenerator/tree/main/docs/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/uniongenerator-social-card.jpg',
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: false,
    },
    navbar: {
      title: 'UnionGenerator',
      logo: {
        alt: 'UnionGenerator Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/selcukgural/UnionGenerator',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/intro',
            },
          ],
        },
        {
          title: 'Packages',
          items: [
            {
              label: 'NuGet Gallery',
              href: 'https://www.nuget.org/packages/UnionGenerator',
            },
            {
              label: 'ASP.NET Core',
              href: 'https://www.nuget.org/packages/UnionGenerator.AspNetCore',
            },
            {
              label: 'Entity Framework',
              href: 'https://www.nuget.org/packages/UnionGenerator.EntityFrameworkCore',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/selcukgural/UnionGenerator',
            },
            {
              label: 'Issues',
              href: 'https://github.com/selcukgural/UnionGenerator/issues',
            },
            {
              label: 'Releases',
              href: 'https://github.com/selcukgural/UnionGenerator/releases',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} UnionGenerator. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.vsDark,
      additionalLanguages: ['csharp', 'bash', 'json', 'diff'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
