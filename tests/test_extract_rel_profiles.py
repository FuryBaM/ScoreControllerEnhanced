import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
from extract_rel_profiles import extract, hash_dictionary, joaat


SAMPLE = """<?xml version="1.0"?>
<Dat151><Items>
 <Item type="StartTrackAction"><Name>hei4_fin_start_sta</Name><Tracks>
  <Item><Track>hei4_fin_track_m05</Track></Item>
 </Tracks></Item>
 <Item type="SetMoodAction"><Name>hei4_fin_action</Name>
  <Mood>hei4_fin_action_md</Mood><FadeIn value="5000"/><FadeOut value="10000"/>
 </Item>
 <Item type="InteractiveMusicMood"><Name>hei4_fin_action_md</Name>
  <FadeIn value="400"/><FadeOut value="3000"/><Moods>
   <Item><StemMix>hash_A46B883B</StemMix><Unk1 value="25"/>
    <Unk2 value="30"/><Unk3 value="8"/><Unk4 value="0"/>
    <Bar/><Beat/></Item>
   <Item><StemMix>sm_1_2_7</StemMix><Unk1 value="10"/><Bar/></Item>
  </Moods>
 </Item>
</Items></Dat151>"""


class MusicProfileExtractorTests(unittest.TestCase):
    def test_joaat_known_rel_hashes(self):
        self.assertEqual(joaat("sm_1_2"), 0xA46B883B)
        self.assertEqual(joaat("sm_1"), 0x8DF4DCF7)
        self.assertEqual(hash_dictionary()["hash_A46B883B"], ("sm_1_2", [1, 2]))

    def test_metadata_only_is_default(self):
        with tempfile.TemporaryDirectory() as d:
            source = Path(d) / "rel.xml"
            source.write_text(SAMPLE, encoding="utf-8")
            package = extract(source, "hei4_fin_track_m05", "hei4_fin_",
                              "CayoFinM05REL")
        self.assertIsNone(package["Track"]["Event"])
        self.assertEqual(package["Track"]["EventVerification"], "unmapped")
        event = package["Profile"]["Events"][0]
        self.assertIsNone(event["Event"])
        self.assertEqual(event["Mood"], "hei4_fin_action_md")
        self.assertEqual(event["FadeInMs"], 5000)
        self.assertEqual(event["FadeOutMs"], 10000)
        self.assertEqual(event["MoodFadeInMs"], 400)
        self.assertEqual(event["MoodFadeOutMs"], 3000)
        self.assertEqual(event["StemMixes"][0]["Stems"], [1, 2])
        self.assertEqual(event["StemMixes"][1]["Stems"], [1, 2, 7])

    def test_candidate_events_must_be_explicit(self):
        with tempfile.TemporaryDirectory() as d:
            source = Path(d) / "rel.xml"
            source.write_text(SAMPLE, encoding="utf-8")
            package = extract(source, "hei4_fin_track_m05", "hei4_fin_",
                              "CayoFinM05REL", candidate_events=True,
                              collection_name="CayoPerico",
                              collection_title="The Cayo Perico Heist")
        self.assertEqual(package["CollectionName"], "CayoPerico")
        self.assertEqual(package["Track"]["Event"], "hei4_fin_start_sta")
        self.assertEqual(package["Profile"]["Events"][0]["Event"], "hei4_fin_action")
        self.assertEqual(package["Profile"]["Events"][0]["Verification"], "candidate")

    def test_unknown_track_is_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            source = Path(d) / "rel.xml"
            source.write_text(SAMPLE, encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "absent"):
                extract(source, "missing", "hei4_fin_", "X")


if __name__ == "__main__":
    unittest.main()
