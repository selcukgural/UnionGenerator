import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'UnionGenerator',
  tagline: 'Powerful Discriminated Unions for C# with Source Generators',
  favicon: 'img/favicon.ico',

  // SEO and social media
  url: 'https://selcukgural.github.io',
  baseUrl: '/UnionGenerator/',
  
  // Social media meta
  organizationName: 'selcukgural',
  projectName: 'UnionGenerator',

  // Future flags
  future: {
    v4: true,
  },

  onBrokenLinks: 'warn',

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
          routeBasePath: '/', // Serve docs at the site's root
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
    // Social card image
    image: 'img/uniongenerator-social-card.jpg',
    
    // Metadata
    metadata: [
      {name: 'keywords', content: 'C#, discriminated unions, source generator, pattern matching, functional programming, type safety'},
      {name: 'author', content: 'Selçuk Güral'},
      {name: 'twitter:card', content: 'summary_large_image'},
      {name: 'twitter:title', content: 'UnionGenerator - Discriminated Unions for C#'},
      {name: 'twitter:description', content: 'Compile-time discriminated unions for C# with zero runtime overhead. Type-safe pattern matching and exhaustive case handling.'},
      {property: 'og:type', content: 'website'},
      {property: 'og:site_name', content: 'UnionGenerator'},
    ],
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
          to: '/',
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
              to: '/',
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
