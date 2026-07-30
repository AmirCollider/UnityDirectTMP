## What this changes

<!-- What the code does differently now, and why. If it fixes a bug, what the
     bug actually was — the CHANGELOG entry is written from this. -->

## Checks

- [ ] `python3 .github/scripts/validate_package.py` passes
- [ ] EditMode tests pass (Window → General → Test Runner)
- [ ] New scripts have their `.meta` file committed
- [ ] New user-facing strings exist in English, Japanese and Persian
- [ ] `CHANGELOG.md` updated if this is user-visible

## If this touches the Editor-font feature

- [ ] A font that cannot draw basic Latin is still **refused**, not warned about
- [ ] Revert still restores Unity's own font, and quitting Unity still undoes everything

## Anything reviewers should look at first

<!-- Optional. A decision you were unsure about, a trade-off you took. -->
