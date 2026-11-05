# Text Transformer

A small, dependency-free JavaScript library and command-line tool for common text transformations.

## Requirements

Node.js 18 or newer.

## Library

```js
import {
  invertCase,
  sentenceCase,
  titleCase,
  transformText,
} from "text-transformer";

titleCase("hELLO, WORLD!"); // "Hello, World!"
sentenceCase("hELLO WORLD. HOW ARE YOU?"); // "Hello world. How are you?"
invertCase("Hello, 123!"); // "hELLO, 123!"
transformText("hello", "uppercase"); // "HELLO"
```

Available functions are `upperCase`, `lowerCase`, `invertCase`, `titleCase`,
`sentenceCase`, `trim`, and `collapseWhitespace`. `transformText` accepts one of
`uppercase`, `lowercase`, `invert`, `title`, `sentence`, `trim`, or
`collapse-whitespace`.

Case conversion uses JavaScript's Unicode-aware string casing. Title case
capitalizes each word, lowercases its remaining letters, and treats hyphens and
punctuation as word separators. Sentence case lowercases the text and
capitalizes the first letter and letters after `.`, `!`, or `?` followed by
whitespace. `collapseWhitespace` replaces each run of whitespace with one space
without trimming the ends.

## CLI

```sh
npx text-transform title "hELLO, WORLD!"
echo "hello world" | npx text-transform uppercase
```

Run `text-transform --help` for the supported operations.

## Development

```sh
npm test
```
