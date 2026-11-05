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
    .replace(/(^|[.!?]\s+)(\p{L})/gu, (_match, boundary, letter) =>
      boundary + letter.toUpperCase(),
    );
}

export function trim(text) {
  return text.trim();
}

export function collapseWhitespace(text) {
  return text.replace(/\s+/gu, " ");
}

export const TRANSFORMATIONS = Object.freeze({
  uppercase: upperCase,
  lowercase: lowerCase,
  invert: invertCase,
  title: titleCase,
  sentence: sentenceCase,
  trim,
  "collapse-whitespace": collapseWhitespace,
});

export function transformText(text, operation) {
  if (!Object.hasOwn(TRANSFORMATIONS, operation)) {
    throw new RangeError(`Unknown transformation: ${operation}`);
  }

  return TRANSFORMATIONS[operation](text);
}
