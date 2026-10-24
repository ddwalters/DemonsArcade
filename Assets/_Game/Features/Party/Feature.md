# Characters and party

## Current

- `CharacterRecord` holds each character's current data and item containers, whether or not a scene actor exists.
- `PartyManager` tracks the roster, order, leader, and registered records. The current limit is three members, including the player.
- `PartyMemberActor` binds a companion scene object to its record. The player binds to the reserved `player` record.
- Game definitions are under `Resources/Party`. Each requires a unique link to a Node Tree character.
- Character names and default portraits come from Node Tree. The player's playthrough name can override their displayed name.
- Companion records, roster, and equipment are included in save data; dismissed characters retain their records.

## Agreed

- Keep one runtime record per character, independent of the scene actor and party membership.
- Node Tree owns dialogue identity and artwork; game definitions contain game-specific settings.
- Do not duplicate names or portraits. Require the matching Node Tree character to be selected.
- Party membership does not implement following or other companion AI.

## Unfinished

- Recruitment currently has APIs and component context-menu actions, rather than a player-facing recruitment flow.
- Character naming and additional party management interactions can be expanded later.
- Verify scene changes, dismissal/recruitment, and save restoration in Play Mode after the ownership changes.
