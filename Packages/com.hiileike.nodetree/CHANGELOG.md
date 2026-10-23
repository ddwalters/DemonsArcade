# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.4.3] - 2026-10-23

### Added
- Dialog history: `DialogRunner.History` records every line spoken and every option the player picked, plus the ordered list of conversations (one per node tree started).
- Scrollable history panel (Log and Conversations tabs) built at runtime, opened with the optional `historyAction` input or `DialogRunner.ToggleHistory()`. Continue input is ignored while it is open.
- Back navigation: optional `backAction` (or `DialogRunner.GoBack()`) returns to the previous line when it was a plain Dialog node in the current tree; never into Dialog Options or other node types. `DialogRunner.Advance()` moves forward for UI buttons, and `CanGoBack` reports availability. Replayed lines are not duplicated in History.
- Dialog Settings: a section at the top of the Node Tree window. "Global" edits one project-wide defaults asset (`Assets/Resources/NodeTree/GlobalDialogDefaults.asset`, created on first edit). "This Tree" has a Use Global Defaults checkbox (on by default); unchecking it lets the tree hold its own settings. Settings are Is Repeatable, Can Rewind Dialog and a default text style (speed, tick sound, font, size, color, style).
- General foldout on Dialog, Dialog Options and Action nodes with Use Tree Defaults, Is Repeatable and Can Rewind. Dialog nodes use the tree defaults; Options and Action nodes start with their own values (repeatable and rewind off). Priority is node, then tree, then global.
- Is Repeatable: a tree that is not repeatable can only be started once per session; a node that is not repeatable runs once, then later conversations pass through its first output. Can Rewind: back navigation needs the tree and the target Dialog node to allow it.

### Changed
- Node text style now lives in the shared settings system. Nodes saved earlier with a custom speed, tick sound, font, size, color or style keep them as their own node settings. `NodeTreeGraphSO.defaultTextSpeed` is superseded by the tree settings but still read for older trees that use global defaults.
- Dialog Options nodes always show the speaker on the left (the Side field is locked), since the option buttons cover the right portrait. Previously saved Right values are treated as Left.

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
