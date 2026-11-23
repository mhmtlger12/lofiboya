using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Hikaye kitaplığı. Her kitap: kapak rengi, önerilen yaş, kapak sahnesi ve sayfa sahneleri.
    /// Metinler BookStrings.cs içinde (anahtar: bk_{id}_t başlık, bk_{id}_{n} sayfa n, n = 1'den başlar).
    /// Resimler StoryArt ile kodla çizilir; sahne dili:
    ///   "zemin[:ayar]; öğe x y boy [bayraklar]; öğe x y boy ..."
    ///   x, y: resmin sol alt köşesine göre 0..1 (y yukarı). boy: resim yüksekliğine oranla.
    ///   bayraklar: f (aynala), happy / sad / sleep / wow (yüz ifadesi), n=3 (sayı ayarı), c=FF8899 (renk)
    /// Masalların hepsi kamu malı (Ezop, Andersen, halk masalı) ya da bu oyun için yazılmış özgün hikayelerdir;
    /// metinler kendi sözlerimizle yeniden anlatıldı, resimler bize ait.
    /// </summary>
    public static class Books
    {
        public class Book
        {
            public string Id;
            public Color Cover;
            public int Age;            // 0: 3–4, 1: 5–6, 2: 7+ (SaveSystem.AgeBand ile aynı)
            public string CoverScene;
            public string[] Scenes;    // her sayfanın resmi
            public int Pages => Scenes.Length;
            public string Title => Loc.T("bk_" + Id + "_t");
            public string Text(int page) => Loc.T("bk_" + Id + "_" + page);   // page: 1..Pages
            public bool Read
            {
                get => PlayerPrefs.GetInt("book_" + Id, 0) == 1;
                set { PlayerPrefs.SetInt("book_" + Id, value ? 1 : 0); PlayerPrefs.Save(); }
            }
        }

        static Book B(string id, string cover, int age, string coverScene, params string[] scenes)
            => new Book { Id = id, Cover = Theme.Hex(cover), Age = age, CoverScene = coverScene, Scenes = scenes };

        public static int ReadCount
        {
            get { int n = 0; foreach (var b in All) if (b.Read) n++; return n; }
        }

        public static Book Find(string id) => All.Find(b => b.Id == id);

        public static readonly List<Book> All = new()
        {
            B("hare", "6FBF8E", 0,
                "meadow; flag .82 .2 .5; hare .3 .12 .5 happy; turtle .6 .1 .34 happy; flower .1 .12 .16 c=FF8FAB; flower .93 .1 .14 c=FFD166",
                "meadow; tree .85 .3 .62; hare .32 .12 .52 happy; turtle .62 .1 .32; flower .12 .1 .16 c=FF8FAB; tuft .48 .1 .1",
                "meadow; flag .14 .22 .48; hare .52 .12 .5 happy f; turtle .3 .1 .3 happy f; tuft .8 .1 .1; flower .9 .12 .15 c=A0C4FF",
                "meadow; tree .7 .26 .7; hare .7 .1 .44 sleep; zz .82 .55 .14; turtle .2 .14 .2; flower .4 .1 .13 c=FFD166",
                "meadow; tree .22 .3 .56; hare .2 .17 .3 sleep; zz .28 .45 .1; turtle .62 .1 .36 happy; flower .88 .1 .16 c=FF8FAB",
                "meadow; flag .62 .2 .58; turtle .66 .1 .34 happy; hare .24 .12 .46 wow; spark .72 .62 .12; spark .5 .7 .08; balloon .9 .45 .3 c=FF8FAB"),

            B("lion", "E9A23B", 0,
                "forest; lion .42 .1 .66 happy; mouse .74 .1 .22 happy; heart .6 .62 .1",
                "forest; lion .4 .1 .62 sleep; zz .55 .66 .12; mouse .73 .1 .2 wow; mushroom .9 .1 .14",
                "forest; lion .4 .1 .64; mouse .7 .12 .2 sad; tuft .1 .1 .1",
                "forest; lion .4 .1 .64 happy; mouse .74 .1 .22 happy; spark .62 .5 .08",
                "forest; lion .45 .1 .6 sad; net .45 .08 .7; tuft .9 .1 .1",
                "forest; lion .4 .1 .64 happy; rope .7 .08 .16; mouse .72 .1 .22 happy; heart .56 .74 .1; heart .66 .66 .07"),

            B("ant", "C0784E", 1,
                "meadow; cricket .3 .12 .32 happy; ant .68 .12 .26 happy; wheat .85 .12 .3 n=2; note .22 .55 .08",
                "meadow; sun .85 .82 .3; cricket .42 .12 .38 happy; note .3 .55 .08; note .56 .62 .07; flower .85 .1 .15 c=FFD166",
                "meadow; anthill .78 .12 .42; ant .44 .12 .28 f; wheat .5 .14 .2 n=1; flower .15 .1 .14 c=FF8FAB",
                "meadow; cricket .3 .12 .36 happy; note .22 .54 .07; ant .68 .12 .28 f; wheat .76 .14 .2 n=1",
                "snow; cricket .45 .12 .38 sad; anthill .8 .14 .42 n=1",
                "snow; anthill .62 .14 .58 n=1; ant .5 .13 .26 happy; cricket .25 .12 .34 happy; heart .38 .6 .1; note .2 .55 .07"),

            B("crow", "5C6B9E", 1,
                "meadow; sun .85 .8 .28; jug .62 .12 .4 n=3; crow .34 .12 .42 happy",
                "meadow; sun .82 .8 .36 happy; crow .45 .12 .44 sad; tuft .8 .1 .1",
                "meadow; sun .88 .82 .26; jug .62 .12 .42 n=0; crow .32 .12 .42",
                "meadow; jug .65 .12 .42 n=0; crow .34 .12 .42 happy; spark .36 .62 .1; spark .24 .55 .06",
                "meadow; jug .62 .12 .42 n=2; crow .34 .12 .42 happy; pebble .18 .1 .14 n=3",
                "meadow; jug .6 .12 .42 n=3; crow .36 .12 .42 happy; heart .48 .66 .08"),

            B("duckling", "7FB6E0", 1,
                "pond; swan .5 .16 .52 happy; duck .2 .14 .2 happy; duck .82 .14 .2 happy f",
                "pond; nest .5 .14 .4 n=3; duck .24 .14 .2 happy; duck .78 .14 .2 happy f; gduck .58 .2 .22 wow",
                "pond; gduck .25 .14 .26 sad; duck .62 .14 .2 happy; duck .76 .14 .2 happy; duck .9 .14 .2 happy",
                "snow; gduck .5 .14 .28 sad; pine .15 .18 .5 n=1; pine .86 .2 .44 n=1",
                "pond:spring; swan .45 .16 .5 wow; spark .62 .7 .1; flower .9 .1 .15 c=FF8FAB",
                "pond:spring; swan .3 .16 .46 happy; swan .58 .16 .48 happy f; swan .84 .17 .4 happy f; heart .45 .76 .08"),

            B("hen", "D9534F", 1,
                "farm; hen .5 .12 .44 happy; wheat .25 .12 .34 n=3; bread .8 .1 .2",
                "farm; hen .48 .12 .42; cat .16 .12 .34 f; dog .8 .12 .36 f; mouse .92 .1 .18 f",
                "farm; wheat .3 .12 .5 n=4; hen .66 .12 .42 happy f; sun .86 .84 .24",
                "farm; hen .55 .12 .42 f; wheat .7 .14 .26 n=2; cat .18 .12 .3 sleep; dog .34 .12 .3 sleep; zz .28 .45 .1",
                "home; bread .52 .14 .28; hen .26 .12 .44 happy; spark .6 .5 .08",
                "home; bread .5 .14 .26; hen .3 .12 .4 happy; cat .66 .12 .32 happy; dog .84 .12 .34 happy f; mouse .12 .1 .18 happy; heart .5 .62 .08"),

            B("bear", "4B4F8C", 0,
                "home:night; bedhead .5 .1 .8; bear .5 .14 .46 sleep; bed .5 .1 .8; zz .7 .62 .12",
                "home:night; bedhead .5 .1 .8; bear .5 .14 .46; bed .5 .1 .8",
                "home:night; bedhead .4 .1 .7; bear .4 .14 .42 wow; bed .4 .1 .7; owl .84 .62 .3 happy",
                "night; branch .7 .52 .7; owl .74 .55 .3 happy; bear .3 .12 .44 happy; star .2 .82 .07; star .4 .9 .06",
                "home:night; bedhead .5 .1 .8; bear .5 .14 .46 happy; bed .5 .1 .8; star .2 .82 .07; star .82 .86 .06; star .66 .9 .05",
                "home:night; bedhead .5 .1 .8; bear .5 .14 .46 sleep; bed .5 .1 .8; zz .7 .6 .12; moon .5 .86 .14 n=4 sleep"),

            B("leaf", "4E9F6A", 0,
                "rain; leaf .5 .5 .7; mouse .3 .12 .22 happy; frog .5 .12 .26 happy; bunny .72 .12 .34 happy",
                "rain; leaf .4 .5 .66; mouse .4 .12 .24",
                "rain; leaf .45 .5 .66; mouse .36 .12 .22 happy; frog .72 .12 .24 sad",
                "rain; leaf .5 .5 .72; mouse .28 .12 .2 happy; frog .46 .12 .24 happy; bunny .66 .12 .3 happy; hedgehog .86 .12 .24 wow",
                "rain; leaf .5 .5 .72; mouse .28 .12 .2 happy; frog .44 .12 .24 happy; bunny .62 .12 .3 happy; hedgehog .8 .12 .24 happy; note .5 .78 .07",
                "meadow; rainbow .5 .26 1; mouse .22 .12 .2 happy; frog .4 .12 .24 happy; bunny .6 .12 .32 happy; hedgehog .8 .12 .24 happy"),

            B("seed", "F2B544", 0,
                "garden; sprout .6 .14 .7 n=3; bunny .3 .12 .4 happy; sun .86 .84 .24",
                "garden; sprout .6 .14 .3 n=0; bunny .34 .12 .42 happy",
                "garden; can .7 .12 .22; sprout .52 .14 .3 n=0; bunny .3 .12 .42 sad",
                "garden; sun .22 .82 .28; cloud .7 .82 .26 n=1; sprout .52 .14 .3 n=1; bunny .8 .12 .36",
                "garden; sprout .55 .14 .4 n=2; bunny .3 .12 .42 wow; spark .62 .5 .08",
                "garden; sprout .6 .14 .74 n=3; bunny .3 .12 .42 happy; sun .88 .84 .22; heart .45 .62 .07"),

            B("fish", "3AA6C9", 0,
                "under; fish .45 .45 .3 happy c=FF9F43; crab .2 .1 .2 happy; star5 .78 .1 .2 happy; turtle .78 .6 .26 happy",
                "under; fish .5 .45 .34 happy c=FF9F43; bubble .62 .7 .06; bubble .66 .78 .04",
                "under; fish .62 .5 .3 happy c=FF9F43 f; crab .3 .1 .26 happy",
                "under; fish .3 .52 .28 happy c=FF9F43; star5 .66 .1 .26 happy",
                "under; fish .25 .45 .26 happy c=FF9F43; turtle .64 .45 .38 happy f",
                "under; fish .45 .5 .26 happy c=FF9F43; crab .18 .1 .2 happy; star5 .82 .1 .2 happy; turtle .78 .66 .24 happy f; bubble .5 .76 .05"),

            B("hedgehog", "B5838D", 0,
                "meadow; hedgehog .4 .12 .34 happy; blanket .4 .12 .34; heart .58 .58 .1",
                "meadow; hedgehog .34 .12 .32 sad; bunny .66 .12 .4 wow",
                "meadow; tree .7 .28 .7; hedgehog .62 .12 .3 sad; flower .18 .1 .14 c=FFADAD",
                "meadow; bunny .32 .12 .4 happy; bear .62 .12 .46 happy; blanket .48 .18 .4",
                "meadow; tree .76 .28 .66; hedgehog .6 .12 .32 happy; blanket .6 .12 .32",
                "meadow; bunny .24 .12 .4 happy; hedgehog .48 .12 .32 happy; blanket .48 .12 .32; bear .72 .12 .46 happy f; heart .48 .62 .1"),

            B("kite", "E76F51", 1,
                "sky; kite .66 .7 .3; fox .3 .12 .4 happy; squirrel .52 .12 .3 happy",
                "sky; kite .7 .74 .3; fox .28 .12 .42 happy; squirrel .48 .12 .32 happy",
                "meadow; tree .62 .28 .62; kite .66 .66 .2 n=0; fox .24 .12 .4 sad; squirrel .4 .12 .3 wow",
                "meadow; tree .62 .28 .62; kite .66 .66 .2 n=0; squirrel .54 .55 .22; fox .22 .12 .4 wow",
                "meadow; tree .62 .28 .62; kite .66 .66 .2 n=0; owl .78 .7 .22 happy f; fox .22 .12 .4 happy",
                "sky; kite .7 .76 .3; fox .24 .12 .4 happy; squirrel .44 .12 .3 happy; owl .62 .12 .3 happy"),

            B("snowman", "6C8EBF", 0,
                "snow; snowman .5 .12 .62 n=1; bear .22 .12 .42 happy; bunny .8 .12 .38 happy",
                "snow; snowman .5 .12 .62 n=1; bear .22 .12 .42 happy; bunny .8 .12 .38 happy",
                "snow; snowman .4 .12 .6 n=0 wow; hat .76 .72 .18 n=1; cloud .72 .86 .2",
                "snow; tree .72 .3 .66 n=0; hat .74 .58 .12; bird .76 .62 .1; bear .26 .12 .4 wow; bunny .42 .12 .36",
                "snow; tree .55 .3 .7 n=0; hat .57 .58 .16; bird .58 .63 .14 happy; heart .68 .74 .06",
                "snow; snowman .4 .12 .6 n=2 happy; bear .7 .12 .4 happy; bunny .88 .12 .34 happy; hat .14 .14 .14; bird .14 .18 .12 happy"),

            B("frog", "6BBF59", 0,
                "pond; lily .5 .15 .3; frog .5 .14 .34 happy; lily .2 .16 .16; lily .82 .17 .16",
                "pond; sun .78 .8 .32 happy; lily .3 .15 .28; frog .3 .14 .32 happy",
                "pond; duck .55 .18 .22 happy; duck .76 .18 .22 happy f; lily .22 .15 .26; frog .22 .14 .3 happy",
                "pond; butterfly .4 .62 .16 c=FF8FAB; butterfly .6 .72 .14 c=A0C4FF; butterfly .8 .6 .16 c=FFD166; lily .18 .15 .26; frog .18 .14 .3 happy",
                "pond; lily .35 .2 .14; lily .55 .17 .14; lily .72 .22 .12; lily .88 .16 .14; lily .14 .15 .26; frog .14 .14 .3 happy",
                "pond; fish .4 .18 .1 c=FF9F43; fish .55 .14 .1 c=FFD166 f; fish .66 .22 .09 c=A0C4FF; fish .78 .15 .1 c=FF8FAB f; fish .9 .2 .09 c=B9FBC0; lily .16 .15 .26; frog .16 .14 .3 happy"),

            B("snail", "9D7BD8", 1,
                "sunset; snail .48 .12 .32 happy; flower .15 .1 .16 c=FFADAD; flower .86 .1 .15 c=FFD166",
                "meadow; rainbow .7 .36 .5; snail .3 .12 .3 happy",
                "meadow; tuft .2 .1 .14; snail .5 .12 .28 happy; tuft .7 .1 .14; flower .88 .1 .16 c=BDB2FF",
                "meadow; snail .36 .12 .28 happy; ladybug .66 .14 .16 wow; flower .74 .1 .2 c=FFD166",
                "garden; snail .3 .12 .28 happy; flower .55 .1 .26 c=FF8FAB; butterfly .7 .6 .16 c=A0C4FF; flower .82 .1 .2 c=FFD166",
                "sunset; snail .5 .16 .32 happy; spark .7 .7 .08"),

            B("ladybug", "E5484D", 0,
                "garden; ladybug .5 .14 .4 happy n=7; flower .2 .1 .24 c=FFD166; flower .82 .1 .22 c=BDB2FF",
                "garden; ladybug .5 .14 .44 sad n=6; flower .84 .1 .2 c=FFD166",
                "garden; ladybug .3 .14 .34 sad n=6; bee .68 .4 .3",
                "garden; ladybug .3 .14 .34 n=6; butterfly .68 .5 .32 c=A0C4FF",
                "garden; ladybug .5 .14 .46 happy n=7; spark .7 .6 .1; spark .3 .66 .07",
                "garden; ladybug .5 .14 .36 happy n=7; bee .22 .46 .24 happy; butterfly .8 .52 .26 happy c=A0C4FF"),

            B("bee", "F4C430", 0,
                "garden; bee .4 .55 .3 happy; hive .78 .3 .4; flower .2 .1 .22 c=FF8FAB",
                "garden; sun .82 .82 .3 happy; bee .4 .5 .32 happy; flower .66 .1 .2 c=FFADAD",
                "garden; bee .5 .55 .26 happy; flower .3 .1 .3 c=FF8FAB; flower .72 .1 .26 c=BDB2FF",
                "garden; hive .55 .26 .48; bee .26 .5 .24 happy",
                "garden; hive .66 .26 .46; bear .3 .12 .5 happy; bee .74 .68 .16 wow",
                "garden; bear .32 .12 .5 happy; honey .5 .12 .16; bee .66 .5 .22 happy; flower .84 .1 .2 c=FF8FAB"),

            B("moon", "3D3B73", 1,
                "night; moon .66 .7 .3 n=4 happy; mouse .3 .12 .24 happy; owl .82 .12 .26 happy",
                "night; moon .62 .72 .3 n=4 happy; mouse .3 .12 .26 happy",
                "night; moon .62 .72 .3 n=2; mouse .3 .12 .26 wow",
                "night; moon .62 .72 .3 n=0; mouse .32 .12 .26 sad",
                "night; moon .66 .74 .26 n=1; branch .76 .48 .6; owl .78 .5 .26 happy f; mouse .26 .12 .26",
                "night; moon .62 .72 .32 n=4 happy; mouse .26 .12 .26 happy; owl .78 .12 .28 happy f"),

            B("puppy", "F08A9B", 0,
                "home; cake .5 .14 .32 n=3; dog .24 .12 .42 happy; balloon .8 .4 .4 c=A0C4FF; balloon .9 .36 .34 c=FFD166",
                "home; dog .48 .12 .44 sad",
                "garden; ball .66 .12 .14; dog .38 .12 .42",
                "home; balloon .2 .36 .4 c=FF8FAB; balloon .1 .32 .34 c=A0C4FF; cat .38 .12 .32 happy; dog .58 .12 .42 wow; bear .82 .12 .44 happy f",
                "home; cake .5 .14 .32 n=3; cat .2 .12 .3 happy; dog .8 .12 .4 happy f",
                "home; cake .5 .14 .28 n=3; dog .3 .12 .42 happy; gift .7 .12 .18 c=A0C4FF; gift .86 .12 .15 c=FF8FAB; heart .5 .64 .08; balloon .1 .36 .36 c=FFD166"),

            B("seasons", "8DB580", 1,
                "meadow; tree .5 .26 .68 n=1; squirrel .22 .12 .3 happy; flower .85 .1 .14 c=FF8FAB",
                "meadow; tree .52 .26 .68 n=1; squirrel .26 .12 .3 happy; flower .82 .1 .14 c=FF8FAB",
                "meadow; tree .52 .26 .68 n=2; squirrel .55 .12 .28 sleep; zz .62 .38 .08",
                "meadow:autumn; tree .5 .26 .68 n=3; squirrel .26 .12 .3 happy; acorn .4 .1 .08; acorn .46 .1 .06",
                "snow; tree .55 .28 .68 n=0; squirrel .28 .12 .3 happy; acorn .38 .12 .07",
                "meadow; tree .52 .26 .68 n=1; squirrel .24 .12 .3 happy; bird .74 .64 .1 happy f; flower .88 .1 .14 c=FFD166"),

            B("birthday", "8E7CC3", 1,
                "home; gift .5 .12 .3 c=FF8FAB; cat .22 .12 .32 happy; bunny .78 .12 .38 happy f; balloon .1 .38 .34 c=A0C4FF",
                "meadow; cat .35 .12 .36 sad; flower .7 .1 .16 c=FFD166",
                "meadow; cat .3 .12 .34; flower .5 .1 .18 c=FF8FAB; flower .62 .1 .16 c=FFD166; flower .74 .1 .18 c=BDB2FF",
                "home; cat .3 .12 .34 happy; bunny .7 .12 .4 wow f",
                "home; gift .5 .12 .26 c=FF8FAB; bunny .74 .12 .4 happy f; cat .26 .12 .34 wow",
                "home; vase .5 .12 .3; cat .26 .12 .34 happy; bunny .76 .12 .4 happy f; heart .5 .66 .08"),
        };
    }
}
