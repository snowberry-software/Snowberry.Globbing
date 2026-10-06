'use strict';

// Generates picomatch-cases.json: the expected match results of the pinned picomatch commit,
// used as the oracle for the .NET, PostgreSQL and JavaScript conformance tests.
// Usage: npm ci && npm run generate

const fs = require('fs');
const path = require('path');
const picomatch = require('picomatch');

const lock = require('./package-lock.json');
const commit = lock.packages['node_modules/picomatch'].resolved.split('#').pop();

const patterns = [
  // stars and globstars
  '*', '*.js', '*.*', '.*', '*/*', '**', '**/*', '**/*.js', '**/.*', '**/*.*', './*.js', '.*.js', '*.js.map',
  '**.js', 'a/*.js', 'a/**/b', 'a/**', '**/b', 'a/*/b', 'a/**/*.js', '**/node_modules/**', '**/*.min.js',
  '**/.git/**', 'a*b', '*a', 'a**', '/a', '/**', 'foo/', '*/', '**/',
  // question marks and literals
  '?', '??', 'a?c', '*.?s', 'foo', 'a/b', 'a.b', 'a+b', 'a$b', 'a^b', '(a)', 'a|b', 'a/./b', 'a/../b', '..', '.',
  // braces
  '*.{js,ts}', '{a,b}/*.js', 'a{,b}c', '{1..3}', 'a/{b,c}/b', '{a,b}', 'foo{1,2}bar', 'a{b', 'a}b', '{}', 'a{1}',
  // brackets and POSIX classes
  '[abc]', '[a-c]*', '[!abc]', '[^abc]', '[!]', '[]]', '[\\]]', '[\\]a]', 'a[\\]b]c', '[\\[\\]]', '[!\\]]',
  '[a\\-z]', '[-a]', '[a-]', '[*]', '[?]', 'foo[/]bar', '[[:alpha:]]', '[[:digit:]]*', '[[:alpha:][:digit:]]',
  '[![:alpha:]]', '[[:punct:]]', '[[:space:]]', '[[:upper:][:lower:]]',
  // extglobs
  '!(*.md)', '*(a|b)', '+(a|b)', '?(a|b)', '@(a|b)', 'a!(b)c', '!(foo)', '*.!(js)', '+([[:alpha:].])',
  '!(*.d).ts', '@(*.js|*.ts)', 'src/**/!(*.test).{js,ts}',
  // negation
  '!*.md', '!a/*', '!**/*.js', '!!a',
  // escapes
  '\\*', 'a\\*b', '\\?', '\\[a\\]', 'a\\\\b', '\\!a',
];

const inputs = [
  '', 'a', 'b', 'c', 'ab', 'abc', 'acb', 'ac', 'abbc', 'A', 'Z', '1', '2', '3', ' ', '\t',
  'a.js', 'b.js', '.js', 'a.ts', 'a.md', 'a.min.js', 'a.js.map', 'a.js/', 'a.js//', 'a.d.ts', 'foo.d.ts',
  'a/b', 'a/b/', 'a/c/b', 'a/b/c/b', 'a/b.js', 'a/b/c.js', 'a/.b', 'a/.b.js', '.a', '.a.js', '.git/config',
  'src/a.test.js', 'src/a.js', 'src/b/c.ts', 'node_modules/x/a.js', 'x/node_modules/y', '/a', '/a/b', 'ab/c',
  'a.b', 'a+b', 'a$b', 'a^b', '(a)', 'a|b', 'a/./b', 'a/../b', '..', '.',
  '*', '?', '!', '!a', ']', '[', '-', 'a]', '\\', 'a\\b', 'a*b', 'a?c', 'aXb', '[abc]', '{a,b}', '[a]', 'a{b', 'a}b', '{}', 'a{1}',
  'foo', 'foobar', 'foo1bar', 'foo3bar', 'foo/bar', 'foo/',
  // line terminators
  'foo\n', 'a.js\n', 'a\nb', 'x\ny.js', 'a/\nb', 'a\rb', '\r', '\n', 'a\u2028b', '\u2028', 'a\r\n',
];

const optionSets = {
  default: {},
  dot: { dot: true },
  strictSlashes: { strictSlashes: true },
  noglobstar: { noglobstar: true },
  noextglob: { noextglob: true },
  nobrace: { nobrace: true },
};

const cases = [];
for (const [optionSet, options] of Object.entries(optionSets)) {
  for (const pattern of patterns) {
    const regex = picomatch.makeRe(pattern, { windows: false, ...options });
    if (regex.source === '$^')
      continue;

    const matches = [];
    inputs.forEach((input, index) => {
      if (regex.test(input))
        matches.push(index);
    });
    cases.push({ pattern, optionSet, matches });
  }
}

const output = { picomatch: commit, optionSets, inputs, cases };
const file = path.join(__dirname, '..', 'picomatch-cases.json');
fs.writeFileSync(file, JSON.stringify(output, null, 2) + '\n');
console.log(`Wrote ${cases.length} cases x ${inputs.length} inputs (picomatch ${commit}) to ${file}`);
