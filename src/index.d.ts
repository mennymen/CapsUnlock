export type Transformation =
  | "uppercase"
  | "lowercase"
  | "invert"
  | "title"
  | "sentence"
  | "trim"
  | "collapse-whitespace";

export declare const TRANSFORMATIONS: Readonly<
  Record<Transformation, (text: string) => string>
>;

export declare function upperCase(text: string): string;
export declare function lowerCase(text: string): string;
export declare function invertCase(text: string): string;
export declare function titleCase(text: string): string;
export declare function sentenceCase(text: string): string;
export declare function trim(text: string): string;
export declare function collapseWhitespace(text: string): string;
export declare function transformText(
  text: string,
  operation: Transformation,
): string;
