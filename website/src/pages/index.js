import React from 'react';
import Layout from '@theme/Layout';

const sections = [
  {
    title: 'Start',
    body: 'Read the first tutorial and see the core contiguous parser shape in one pass.',
    to: '/docs/',
  },
  {
    title: 'Integrate',
    body: 'See coexistence stories for F#, C#, and existing span-based code.',
    to: '/docs/integrate/how-to',
  },
  {
    title: 'Understand',
    body: 'Read the architecture notes, backend seam, and package-shape explanations.',
    to: '/docs/understand',
  },
  {
    title: 'Model',
    body: 'Move from namespace-first browsing to package hubs, member maps, and source links.',
    to: '/docs/model',
  },
  {
    title: 'Examples',
    body: 'Run focused examples that show one API family at a time and capture the observed output.',
    to: '/docs/examples',
  },
  {
    title: 'Measure',
    body: 'Check the zero-allocation hot paths and the validation strategy behind them.',
    to: '/docs/measure',
  },
];

function card(section) {
  return React.createElement(
    'a',
    {
      className: 'bp-nav-card',
      href: section.to,
      key: section.title,
    },
    React.createElement(
      'span',
      {className: 'bp-nav-card-title'},
      section.title,
    ),
    React.createElement(
      'span',
      {className: 'bp-nav-card-body'},
      section.body,
    ),
  );
}

export default function Home() {
  return React.createElement(
    Layout,
    {
      title: 'BinaryParsec',
      description: 'Binary tokenization and parsing for F#',
    },
    React.createElement(
      'main',
      {className: 'bp-home'},
      React.createElement(
        'section',
        {className: 'bp-hero-shell'},
        React.createElement(
          'div',
          {className: 'bp-hero-copy'},
          React.createElement(
            'p',
            {className: 'bp-eyebrow'},
            'BinaryParsec',
          ),
          React.createElement(
            'h1',
            null,
            'Binary tokenization, parsing, and protocol boundaries.',
          ),
          React.createElement(
            'p',
            {className: 'bp-lede'},
            'BinaryParsec stays F#-first, keeps the contiguous cursor model explicit, and gives protocol packages a thin surface that is easier to read than a raw API dump.',
          ),
          React.createElement(
            'div',
            {className: 'bp-hero-actions'},
            React.createElement(
              'a',
              {className: 'button button--primary', href: '/docs/'},
              'Open the docs',
            ),
              React.createElement(
                'a',
                {className: 'button button--secondary', href: '/docs/examples'},
              'See runnable examples',
            ),
          ),
        ),
        React.createElement(
          'aside',
          {className: 'bp-hero-note'},
          React.createElement('h2', null, 'What the site focuses on'),
          React.createElement(
            'ul',
            null,
            React.createElement('li', null, 'User intent first'),
            React.createElement('li', null, 'Curated package hubs'),
            React.createElement('li', null, 'Executable examples with captured output'),
            React.createElement('li', null, 'Source-aware documentation'),
          ),
        ),
      ),
      React.createElement(
        'section',
        {className: 'bp-grid-shell'},
        ...sections.map(card),
      ),
    ),
  );
}
