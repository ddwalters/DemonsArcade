# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.4.1] - 2026-10-22

### Added
- Portrait Capture window (Tools > Node Tree > Portrait Capture, or from the character editor) that poses a model and saves the frame as a Sprite portrait, optionally adding it to a character as a mood.

## [0.4.0] - 2026-10-22

### Added
- Dialog Owners: a single `DialogOwnerDatabaseSO` (auto-created at `Assets/Resources/NodeTree/DialogOwners.asset`) holding characters with a stable ID, name, default portrait and per-mood portraits.
- Dialog Owners editor window, opened from the new Dialog Owners list (with + and Edit) in the Node Tree window or by opening the database asset.
- Owner, Mood and Side (Left/Right) pickers on Dialog and Dialog Options nodes. Picking an owner reuses the side that owner already has in the tree.
- `DialogRunner` owner name text and left/right portrait images (optional); the non-speaking side is tinted.
- `DialogCanvas` prefab and example scene updated with portrait and name objects.

## [0.1.0] - 2026-05-21

### This is the first release of *\<Node Tree\>*.

*Short description of this release*
