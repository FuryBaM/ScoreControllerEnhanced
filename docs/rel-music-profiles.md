# Original REL music profiles (experimental)

This feature keeps the existing manual \`score_controls.json\` schema unchanged.
Optional files under \`profiles/*.json\` add collections and per-track profiles
at load time. Each file is a package containing \`CollectionName\`,
\`CollectionTitle\`, \`Track\`, and \`Profile\`.

## What is exact, and what still needs validation

A GTA \`dat151.rel.xml\` file describes **StartTrackAction**, **SetMoodAction**,
**InteractiveMusicMood**, **StemMix** and musical transition constraints.
The extractor reconstructs these relationships. Hashes such as
\`hash_A46B883B\` are resolved using JOAAT of names such as \`sm_1_2\`.
Individual mood variants, transition fades and raw selection parameters
are retained.

These facts must not be confused:

- **REL-verified:** a mood/action exists in the decompiled game data; it has
  specific associated stem mixes and fade settings.
- **Native-event candidate:** an action's XML name *might* be accepted through
  \`TRIGGER_MUSIC_EVENT\`. A \`SetMoodAction\` name is not necessarily the name
  of an externally triggerable music event.
- **In-game verified:** a test in GTA confirms the event selects the intended
  score and mood with audible results. Neither XML nor a successful native
  return value alone proves this.
- **Exact individual mix control:** not exposed by this mod. An
  \`InteractiveMusicMood\` can contain multiple \`StemMix\` alternatives, and
  the engine chooses between them. The mod does not directly play FLAC stems,
  change per-stem offsets or override Rockstar's internal music scheduler.

The committed Cayo Perico Finale M05 example is intentionally marked
\`"Verification": "candidate"\` and shows **[test]** beside its moods in the UI.
Treat its action strings as experiments. If rejected, the game now displays
a failure message instead of marking the score as playing. Even when accepted,
listen for a genuine switch before marking a mapping verified.

## Installation

1. Install the original ScoreControllerEnhanced mod as usual.
2. Keep its \`score_controls.json\` at:
   \`<GTA directory>/Score Controller Enhanced/score_controls.json\`.
3. Copy \`profiles/cayo_perico_m05.json\` from this repository into:
   \`<GTA directory>/Score Controller Enhanced/profiles/cayo_perico_m05.json\`.
4. Run the game and open the score controller. The **The Cayo Perico Heist**
   collection contains **Finale M05**. Profile events labelled **[test]**
   are not yet guaranteed to be callable.
5. To compare with the original heist, test start, low, medium, action and
   chase changes, including behavior across bar boundaries and loop points.
   Check the notification if a native trigger returns false.

If a candidate fails, do not rename the XML action randomly. Locate its
actual music event, or leave \`Event\` null with \`Verification\` = \`unmapped\`
and preserve the accurate descriptive metadata.

## Rebuild from a decompiled REL

Python 3.8+; only the standard library is required:

\`\`\`sh
python tools/extract_rel_profiles.py dlchei4_game.dat151.rel.xml \
  --track hei4_fin_track_m05 \
  --prefix hei4_fin_ \
  --name CayoFinM05REL \
  --collection-name CayoPerico \
  --collection-title "The Cayo Perico Heist" \
  --output profiles/cayo_perico_m05.json
\`\`\`

The default is safe **metadata-only**: \`Event\` remains null. To reproduce the
experimental checked-in package, append \`--candidate-events\`. This assigns
the REL action names to the music native for explicit in-game testing.

Run the extractor unit tests with:

\`\`\`sh
python -m unittest discover -s tests -v
\`\`\`

## Cayo Finale M05 example

The uploaded GTA REL has 10 \`SetMoodAction\` states: Suspense Low, Suspense
High, Medium Intensity, Driving, Airborne, Action, Vehicle Chase, Safe Room,
Safe Room Aggro (the XML action contains the typo \`agrro\`), and Silent.

The eight-stem track \`hei4_fin_track_m05\` is referenced by
\`hei4_fin_start_sta\`. There are 28 mood-variant entries. All non-silent
stem mixes in this example resolve to JOAAT-derived \`sm_...\` combinations;
the Silent hash \`hash_F6D980C0\` is intentionally retained unresolved.
The raw \`Unk1\` through \`Unk4\` XML values remain opaque; the importer does
not assign undocumented probability or bar-count semantics to them.

A \`BeatConstraint\` or \`BarConstraint\` defines an internal engine constraint,
not a licence to offset individual stems. The correct timing and transition
behavior remains controlled by Rockstar's audio engine in this mod.

## Future work

- Confirm the externally callable start and mood events in GTA V Enhanced.
- Add an optional event-name mapping table independent of extracted REL
  action names.
- Validate script builds on Windows (.NET Framework 4.8, ScriptHookVDotNet3
  and LemonUI dependencies).
- Import additional game REL files without conflating separate DLC tracks.
