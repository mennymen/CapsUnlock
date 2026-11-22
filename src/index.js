function capitalizeFirstLetter(text) {
  const characters = [...text];
  const firstCasedIndex = characters.findIndex(
    (character) => character.toLowerCase() !== character.toUpperCase(),
  );

  if (firstCasedIndex === -1) {
    return text.toLowerCase();
  }

  return [
    ...characters.slice(0, firstCasedIndex),
    characters[firstCasedIndex].toUpperCase(),
    characters.slice(firstCasedIndex + 1).join("").toLowerCase(),
  ].join("");
}

function splitWords(text) {
  const separated = text
    .replace(/([\p{Ll}\p{N}])(\p{Lu})/gu, "$1 $2")
    .replace(/(\p{Lu})(\p{Lu}\p{Ll})/gu, "$1 $2");

  return separated.match(/[\p{L}\p{N}][\p{L}\p{M}\p{N}]*/gu) ?? [];
}

export function upperCase(text) {
  return text.toUpperCase();
}

export function lowerCase(text) {
  return text.toLowerCase();
}

export function invertCase(text) {
  return [...text]
    .map((character) =>
      character === character.toLowerCase()
        ? character.toUpperCase()
        : character.toLowerCase(),
    )
    .join("");
}

export function titleCase(text) {
  return text.replace(
    /[\p{L}\p{N}][\p{L}\p{M}\p{N}]*(?:['’][\p{L}\p{M}\p{N}]+)*/gu,
    (word) => capitalizeFirstLetter(word),
  );
}

export function sentenceCase(text) {
  return text
    .toLowerCase()
    .replace(/(^|[.!?]\s+|[\r\n\u2028\u2029]+)(\p{L})/gu, (_match, boundary, letter) =>
      boundary + letter.toUpperCase(),
    );
}

export function camelCase(text) {
  const words = splitWords(text).map((word) => word.toLowerCase());
  return words
    .map((word, index) => (index === 0 ? word : capitalizeFirstLetter(word)))
    .join("");
}

export function pascalCase(text) {
  return splitWords(text)
    .map((word) => capitalizeFirstLetter(word))
    .join("");
}

export function snakeCase(text) {
  return splitWords(text)
    .map((word) => word.toLowerCase())
    .join("_");
}

export function kebabCase(text) {
  return splitWords(text)
    .map((word) => word.toLowerCase())
    .join("-");
}

export function trim(text) {
  return text.trim();
}

export function collapseWhitespace(text) {
  return text.replace(/\s+/gu, " ");
}

export function removeBlankLines(text) {
  return text.replace(/(?<=\n|^)[^\S\r\n]*\r?\n/gu, "");
}

export const TRANSFORMATIONS = Object.freeze({
  uppercase: upperCase,
  lowercase: lowerCase,
  invert: invertCase,
  title: titleCase,
  sentence: sentenceCase,
  camel: camelCase,
  pascal: pascalCase,
  snake: snakeCase,
  kebab: kebabCase,
  trim,
  "collapse-whitespace": collapseWhitespace,
  "remove-blank-lines": removeBlankLines,
});

export function transformText(text, operation) {
  if (!Object.hasOwn(TRANSFORMATIONS, operation)) {
    throw new RangeError(`Unknown transformation: ${operation}`);
  }

  return TRANSFORMATIONS[operation](text);
}
