# Changelog

All notable changes to this project will be documented in this file.

## [V9.0.6] - 08-21-2025

### Fixed
- Fixed issue with boolean comparison not being translated correctly in some cases

### Added
- Added support for DateOnly type in queries

## [V9.0.4] - 08-07-2025

### Fixed

- Fixed issue with OFFSET/FETCH parameters not being inlined correctly in complex queries
- Added support for nested queries with Skip/Take

### Changed

- Commented out functionality for casting COUNT in generated queries to INT