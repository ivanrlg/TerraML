# Phase 3F: Merge BandViewer + Training into "Explore & Train"

## Objective

Eliminate the redundant BandViewer page by merging its features (presets, statistics, histogram) into Training. The merged page becomes "Explore & Train" (Step 2 of 4). Add zoom/pan, state persistence across navigation, and inline class creation.

## Motivation

User testing revealed critical UX issues:
- BandViewer duplicates band selection already present in Training
- Navigating between pages loses all training state (drawn regions, samples, band selection)
- No zoom capability for small satellite images
- Adding classes requires leaving Training and going to ProjectSetup

## Scope

- Merge BandViewer features into Training page
- Add state persistence to ProjectStateService
- Implement zoom and pan on training canvas
- Add inline class creation
- Update navigation from 5 to 4 steps
- Auto-select all bands when same dimensions (Copernicus Browser downloads)

## Acceptance Criteria

1. BandViewer page redirects to Training (backwards-compatible URL)
2. Training page shows presets, statistics, histogram from BandViewer
3. Navigating away and back preserves all training state
4. Scroll wheel zooms, Ctrl+drag pans the canvas
5. "+ Add Class" button creates classes without leaving the page
6. Sentinel-2 import auto-selects all bands when dimensions match
7. All existing tests pass, new tests for state persistence
