[![License](https://img.shields.io/github/license/snowberry-software/Snowberry.Globbing)](https://github.com/snowberry-software/Snowberry.Globbing/blob/main/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/Snowberry.Globbing.svg?logo=nuget)](https://www.nuget.org/packages/Snowberry.Globbing/)

Accurate glob pattern matching library for .NET. This library is based on and essentially a C# port of the JavaScript library [picomatch](https://github.com/micromatch/picomatch).

Use this package to match file paths, strings, or any text against glob patterns with support for wildcards, globstars, extglobs, brace expansion, POSIX character classes, and more.

# Usage

Use the static `GlobMatcher` class to match strings against glob patterns:

- Check if a string matches: `bool IsMatch(string str, string pattern, GlobbingOptions? options = null)`
- Create a reusable matcher: `MatcherHandler Create(string pattern, GlobbingOptions? options = null)`
- Parse a pattern: `ParseState Parse(string pattern, GlobbingOptions? options = null)`
- Scan a pattern: `ScanResult Scan(string input, ScanOptions? options = null)`
- Create a regex: `Regex MakeRe(string input, GlobbingOptions? options = null)`

# Features

- **Wildcards**: `*` matches any characters except slashes, `?` matches a single character
- **Globstars**: `**` matches zero or more directories
- **Brace expansion**: `{a,b,c}` expands to match `a`, `b`, or `c`; `{1..5}` expands to `1`, `2`, `3`, `4`, `5`
- **Character classes**: `[abc]` matches `a`, `b`, or `c`; `[a-z]` matches any lowercase letter
- **Negation**: `!pattern` matches anything that doesn't match the pattern
- **Extglobs** (extended glob patterns):
  - `!(pattern)` – match anything except
  - `?(pattern)` – match zero or one
  - `+(pattern)` – match one or more
  - `*(pattern)` – match zero or more
  - `@(pattern)` – match exactly one
- **POSIX character classes**: `[:alnum:]`, `[:alpha:]`, `[:digit:]`, etc.
- **Dotfile handling**: configurable matching of files starting with `.`
- **Cross-platform**: Windows and POSIX path separator support

## Examples

Below are minimal examples demonstrating common usage patterns.

### Basic Matching

```csharp
using Snowberry.Globbing;

// Simple wildcard matching
bool matches = GlobMatcher.IsMatch("file.js", "*.js");        // true
bool noMatch = GlobMatcher.IsMatch("file.txt", "*.js");       // false

// Globstar matching (recursive)
bool deep = GlobMatcher.IsMatch("src/lib/utils.js", "**/*.js"); // true

// Multiple patterns (OR logic)
bool any = GlobMatcher.IsMatch("file.ts", new[] { "*.js", "*.ts" }); // true
```

### Creating Reusable Matchers

```csharp
using Snowberry.Globbing;

// Create a matcher function for repeated use
var isJsFile = GlobMatcher.Create("**/*.js");

isJsFile("app.js");           // true
isJsFile("src/utils.js");     // true
isJsFile("lib/helper.ts");    // false

// Create matcher with multiple patterns
var isSourceFile = GlobMatcher.Create(new[] { "**/*.js", "**/*.ts" });
isSourceFile("component.tsx"); // false
isSourceFile("service.ts");    // true
```

### Using Options

```csharp
using Snowberry.Globbing;

// Case-insensitive matching
var options = new GlobbingOptions { NoCase = true };
GlobMatcher.IsMatch("FILE.JS", "*.js", options); // true

// Match dotfiles
var dotOptions = new GlobbingOptions { Dot = true };
GlobMatcher.IsMatch(".gitignore", "*", dotOptions); // true

// Ignore patterns
var ignoreOptions = new GlobbingOptions
{
    Ignore = new[] { "*.test.js", "*.spec.js" }
};
var matcher = GlobMatcher.Create("**/*.js", ignoreOptions);
matcher("app.js");       // true
matcher("app.test.js");  // false (ignored)

// Windows path support
var winOptions = new GlobbingOptions { Windows = true };
GlobMatcher.IsMatch(@"src\lib\app.js", "src/**/*.js", winOptions); // true
```

### Extglobs (Extended Globs)

```csharp
using Snowberry.Globbing;

// Match one or more
GlobMatcher.IsMatch("aaa", "+(a)");    // true
GlobMatcher.IsMatch("a", "+(a)");      // true

// Match zero or more
GlobMatcher.IsMatch("a", "a*(z)");     // true
GlobMatcher.IsMatch("azz", "a*(z)");   // true

// Match zero or one
GlobMatcher.IsMatch("a", "a?(z)");     // true
GlobMatcher.IsMatch("az", "a?(z)");    // true
GlobMatcher.IsMatch("azz", "a?(z)");   // false

// Match exactly one
GlobMatcher.IsMatch("a", "@(a|b)");    // true
GlobMatcher.IsMatch("c", "@(a|b)");    // false

// Negation extglob
GlobMatcher.IsMatch("a.js", "!(*.txt)");  // true
GlobMatcher.IsMatch("a.txt", "!(*.txt)"); // false
```

### Scanning Patterns

```csharp
using Snowberry.Globbing;

var result = GlobMatcher.Scan("!./foo/**/*.js");

Console.WriteLine(result.Input);    // "!./foo/**/*.js"
Console.WriteLine(result.Base);     // "foo"
Console.WriteLine(result.Glob);     // "**/*.js"
Console.WriteLine(result.Negated);  // true
Console.WriteLine(result.IsGlob);   // true
```

### Match Callbacks

```csharp
using Snowberry.Globbing;

var matchedFiles = new List<string>();
var ignoredFiles = new List<string>();

var options = new GlobbingOptions
{
    Ignore = new[] { "*.min.js" },
    OnMatch = result => matchedFiles.Add(result.Input),
    OnIgnore = result => ignoredFiles.Add(result.Input),
    OnResult = result => Console.WriteLine($"Processed: {result.Input}")
};

var matcher = GlobMatcher.Create("**/*.js", options);
matcher("app.js");      // Added to matchedFiles
matcher("app.min.js");  // Added to ignoredFiles
matcher("readme.md");   // Neither (no match)
```

## Available Options

The `GlobbingOptions` class provides extensive configuration:

| Option       | Default | Description                                                 |
| ------------ | ------- | ----------------------------------------------------------- |
| `BaseName`   | `false` | Match patterns without slashes against basename only        |
| `Bash`       | `false` | Follow bash matching rules more strictly                    |
| `Capture`    | `false` | Use capturing groups in generated regex                     |
| `Contains`   | `false` | Allow pattern to match any substring                        |
| `Dot`        | `false` | Allow patterns to match dotfiles                            |
| `NoCase`     | `false` | Enable case-insensitive matching                            |
| `NoBrace`    | `false` | Disable brace expansion                                     |
| `NoExtglob`  | `false` | Disable extglob patterns                                    |
| `NoGlobstar` | `false` | Disable globstar (`**`) matching                            |
| `NoNegate`   | `false` | Disable negation patterns                                   |
| `Posix`      | `false` | Enable POSIX character classes                              |
| `Windows`    | `null`  | Treat backslashes as path separators (auto-detects if null) |
| `Ignore`     | `null`  | Array of patterns to exclude from matches                   |
| `MaxLength`  | `65536` | Maximum pattern length (ReDoS protection)                   |

See the unit tests in the repository for comprehensive examples of all options and edge cases.
