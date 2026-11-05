import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  collapseWhitespace,
  invertCase,
  lowerCase,
  sentenceCase,
  titleCase,
  transformText,
  trim,
  upperCase,
} from "../src/index.js";

test("upperCase and lowerCase convert Unicode text", () => {
  assert.equal(upperCase("Straße"), "STRASSE");
  assert.equal(lowerCase("HELLO"), "hello");
});

test("invertCase swaps cased characters and preserves uncased characters", () => {
  assert.equal(invertCase("Hello, 123!"), "hELLO, 123!");
  assert.equal(invertCase("Été"), "éTÉ");
});

test("titleCase capitalizes words and lowercases the rest", () => {
  assert.equal(titleCase("  hELLO, WORLD! it's NICE. "), "  Hello, World! It's Nice. ");
  assert.equal(titleCase("добрый ДЕНЬ"), "Добрый День");
});

test("sentenceCase capitalizes sentence starts", () => {
  assert.equal(sentenceCase("hELLO WORLD. HOW ARE YOU?"), "Hello world. How are you?");
  assert.equal(sentenceCase("already lower"), "Already lower");
});

test("trim and collapseWhitespace handle surrounding and repeated spaces", () => {
  assert.equal(trim(" \t hello \n"), "hello");
  assert.equal(collapseWhitespace(" \t hello \n world  "), " hello world ");
});

test("transformText dispatches supported operations and rejects unknown ones", () => {
  assert.equal(transformText("hello", "uppercase"), "HELLO");
  assert.throws(() => transformText("hello", "unknown"), RangeError);
});

test("CLI transforms command-line text and reads piped input", () => {
  const cliPath = fileURLToPath(new URL("../src/cli.js", import.meta.url));
  const fromArguments = spawnSync(
    process.execPath,
    [cliPath, "title", "hello", "world"],
    { encoding: "utf8" },
  );
  assert.equal(fromArguments.status, 0);
  assert.equal(fromArguments.stdout, "Hello World\n");

  const fromStdin = spawnSync(process.execPath, [cliPath, "uppercase"], {
    encoding: "utf8",
    input: "hello",
  });
  assert.equal(fromStdin.status, 0);
  assert.equal(fromStdin.stdout, "HELLO");
});
