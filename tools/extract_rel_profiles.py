#!/usr/bin/env python3
"""Extract track-specific interactive-music profiles from GTA dat151.rel.xml.

The XML describes score actions and stem mixes. It does NOT establish that the
action names are accepted by GTA's TRIGGER_MUSIC_EVENT native. By default
exported actions are metadata-only; --candidate-events enables explicit tests.
"""
import argparse
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


def joaat(value):
    result = 0
    for char in value.lower().encode("utf-8"):
        result = (result + char) & 0xFFFFFFFF
        result = (result + (result << 10)) & 0xFFFFFFFF
        result ^= result >> 6
    result = (result + (result << 3)) & 0xFFFFFFFF
    result ^= result >> 11
    result = (result + (result << 15)) & 0xFFFFFFFF
    return result


def hash_dictionary(count=8):
    lookup = {}
    for mask in range(1, 1 << count):
        stems = [i + 1 for i in range(count) if mask & (1 << i)]
        name = "sm_" + "_".join(map(str, stems))
        lookup["hash_%08X" % joaat(name)] = (name, stems)
    return lookup


def value(element, path, default=None):
    field = element.find(path)
    if field is None:
        return default
    return field.attrib.get("value", field.text) or default


def to_int(text):
    if text is None:
        return None
    try:
        return int(text)
    except (TypeError, ValueError):
        return None


def extract(path, track, prefix, profile_name, candidate_events=False):
    root = ET.parse(path).getroot()
    catalog = {}
    for item in root.findall("./Items/Item"):
        name = item.findtext("Name")
        if name:
            catalog[name] = item

    starts = []
    for name, item in catalog.items():
        if item.get("type") != "StartTrackAction":
            continue
        tracks = [x.text for x in item.findall("./Tracks/Item/Track")]
        if track in tracks:
            starts.append(name)

    hashes = hash_dictionary()
    actions = []
    for name, item in catalog.items():
        if item.get("type") != "SetMoodAction" or not name.startswith(prefix):
            continue
        mood_name = item.findtext("Mood")
        mood = catalog.get(mood_name)
        if mood is None or mood.get("type") != "InteractiveMusicMood":
            continue

        variants = []
        for variant in mood.findall("./Moods/Item"):
            raw = variant.findtext("StemMix") or ""
            if raw in hashes:
                mix_name, stems = hashes[raw]
            elif re.fullmatch(r"sm_(\d+)(?:_\d+)*", raw):
                mix_name = raw
                stems = [int(x) for x in raw[3:].split("_")]
            else:
                mix_name, stems = None, []
            variants.append({
                "Name": mix_name,
                "Stems": stems,
                "RawStemMix": raw,
                "SelectionParams": {
                    key: value(variant, key)
                    for key in ("Unk1", "Unk2", "Unk3", "Unk4")
                },
                "BeatConstraint": variant.findtext("Beat"),
                "BarConstraint": variant.findtext("Bar"),
            })

        title = name[len(prefix):].replace("_", " ").strip().title()
        if title == "Safe Room Agrro":
            title = "Safe Room Aggro"
        actions.append({
            "Name": title,
            "Event": name if candidate_events else None,
            "SourceAction": name,
            "Verification": "candidate" if candidate_events else "unmapped",
            "Mood": mood_name,
            "FadeInMs": to_int(value(item, "FadeIn")),
            "FadeOutMs": to_int(value(item, "FadeOut")),
            "MoodFadeInMs": to_int(value(mood, "FadeIn")),
            "MoodFadeOutMs": to_int(value(mood, "FadeOut")),
            "StemMixes": variants,
        })

    if not starts:
        raise ValueError("Track %r is absent from StartTrackAction lists" % track)
    if not actions:
        raise ValueError("No SetMoodAction matches prefix %r" % prefix)
    return {
        "Track": {
            "Name": track,
            "Title": track.upper(),
            "Event": starts[0] if candidate_events else None,
            "EventVerification": "candidate" if candidate_events else "unmapped",
            "SourceStartActions": starts,
            "ProfileNames": [profile_name],
        },
        "Profile": {
            "Name": profile_name,
            "Title": track.upper() + " (REL)",
            "Source": Path(path).name,
            "Description": "Extracted from GTA dat151; native event mappings require an in-game test.",
            "Events": actions,
        },
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("xml", type=Path, help="decompiled dat151.rel.xml")
    parser.add_argument("--track", required=True)
    parser.add_argument("--prefix", required=True, help="e.g. hei4_fin_")
    parser.add_argument("--name", default="RelMusicProfile")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--candidate-events", action="store_true",
                        help="Use REL action names as TEST candidates; not verified game events")
    args = parser.parse_args()
    data = extract(args.xml, args.track, args.prefix, args.name, args.candidate_events)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("Wrote %s: %d modes, %d unique resolved mix hashes" % (
        args.output, len(data["Profile"]["Events"]),
        len({m["RawStemMix"] for e in data["Profile"]["Events"]
             for m in e["StemMixes"] if m["Name"] is not None})
    ))


if __name__ == "__main__":
    main()
