# Shared UI

## Current

- Shared portrait behavior reads the Node Tree default portrait, with model/initial fallbacks. Model portrait crops use versioned rectangle serialization so their dimensions survive Unity import. The older Canvas prefab remains available alongside GameUI.

## Agreed

- Share reusable presentation components without duplicating gameplay state.

## Unfinished

- Decide whether the older Canvas prefab is still needed before removing it; it has not been deleted.
