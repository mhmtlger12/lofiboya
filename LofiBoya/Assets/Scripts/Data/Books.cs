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
            public bool Sleepy;        // "uyku vakti" rafında da görünür (sakin, akşam masalı)
            public string[] Counts;    // sayma kitabı: her sayfada dokunup sayılacak öğe (ör. "duck")
            public int Pages => Scenes.Length;
            public Book Calm() { Sleepy = true; return this; }
            public Book Count(params string[] items) { Counts = items; return this; }
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
                "home:night; bedhead .4 .1 .7; bear .4 .14 .42 happy; bed .4 .1 .7; owl .82 .43 .28 happy f; star .71 .7 .035; star .87 .73 .03",
                "home:night; bedhead .45 .1 .76; bear .45 .14 .44 happy; bed .45 .1 .76; star .7 .7 .04; star .86 .72 .035; star .77 .52 .03; spark .78 .63 .03",
                "home:night; bedhead .5 .1 .8; bear .5 .14 .46 sleep; bed .5 .1 .8; zz .7 .6 .12").Calm(),

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
                "meadow; bunny .24 .12 .4 happy; hedgehog .48 .12 .32 happy; blanket .48 .12 .32; bear .72 .12 .46 happy f; heart .48 .62 .1").Calm(),

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
                "pond; lily .35 .2 .14; lily .55 .17 .14; lily .74 .21 .13; lily .14 .15 .26; frog .14 .14 .3 happy",
                "pond; fish .4 .18 .1 c=FF9F43; fish .55 .14 .1 c=FFD166 f; fish .66 .22 .09 c=A0C4FF; fish .78 .15 .1 c=FF8FAB f; fish .9 .2 .09 c=B9FBC0; lily .16 .15 .26; frog .16 .14 .3 happy").Count("sun", "duck", "butterfly", "lily", "fish"),

            B("snail", "9D7BD8", 1,
                "sunset; snail .48 .12 .32 happy; flower .15 .1 .16 c=FFADAD; flower .86 .1 .15 c=FFD166",
                "meadow; rainbow .7 .36 .5; snail .3 .12 .3 happy",
                "meadow; tuft .2 .1 .14; snail .5 .12 .28 happy; tuft .7 .1 .14; flower .88 .1 .16 c=BDB2FF",
                "meadow; snail .36 .12 .28 happy; ladybug .66 .14 .16 wow; flower .74 .1 .2 c=FFD166",
                "garden; snail .3 .12 .28 happy; flower .55 .1 .26 c=FF8FAB; butterfly .7 .6 .16 c=A0C4FF; flower .82 .1 .2 c=FFD166",
                "sunset; snail .5 .16 .32 happy; spark .7 .7 .08").Calm(),

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
                "night; moon .62 .72 .32 n=4 happy; mouse .26 .12 .26 happy; owl .78 .12 .28 happy f").Calm(),

            B("puppy", "F08A9B", 0,
                "home; cake .5 .14 .32 n=3; dog .24 .12 .42 happy; balloon .8 .4 .4 c=A0C4FF; balloon .9 .36 .34 c=FFD166",
                "home; dog .48 .12 .44 sad",
                "garden; ball .66 .12 .14; dog .38 .12 .42",
                "home; balloon .2 .36 .4 c=FF8FAB; balloon .1 .32 .34 c=A0C4FF; cat .38 .12 .32 happy; dog .58 .12 .42 wow; bear .82 .12 .44 happy f",
                "home; cake .5 .14 .32 n=3; cat .2 .12 .3 happy; dog .8 .12 .4 happy f",
                "home; cake .5 .14 .28 n=3 sleep; dog .3 .12 .42 happy; gift .7 .12 .18 c=A0C4FF; gift .86 .12 .15 c=FF8FAB; heart .5 .64 .08; balloon .1 .36 .36 c=FFD166"),

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
                "home; cat .3 .12 .34 happy; flower .41 .12 .2 c=FF8FAB; flower .44 .12 .23 c=FFD166; flower .47 .12 .2 c=BDB2FF; bunny .7 .12 .4 wow f",
                "home; gift .5 .12 .26 c=FF8FAB; bunny .74 .12 .4 happy f; cat .26 .12 .34 wow",
                "home; vase .5 .12 .3; cat .26 .12 .34 happy; bunny .76 .12 .4 happy f; heart .5 .66 .08"),

            // ---------------- okul öncesi özgün hikayeler (2. seri, 8 sayfa) ----------------

            B("tidy", "E58FB0", 0,
                "home; bunny .4 .12 .44 happy; ball .74 .12 .16; kite .88 .5 .2 n=0",
                "home; bunny .5 .12 .44 wow; ball .14 .12 .14; kite .3 .5 .2 n=0; blanket .74 .12 .3",
                "home; bunny .45 .12 .44 sad; kite .14 .5 .18 n=0; blanket .76 .12 .3",
                "home; bunny .62 .12 .44 wow f; blanket .24 .12 .3",
                "home; bunny .32 .12 .44 happy; bear .66 .12 .5 happy f; note .5 .7 .07",
                "home; kite .5 .62 .2 n=0; bunny .28 .12 .42 happy; bear .72 .12 .48 happy f; spark .6 .78 .05",
                "home; blanket .3 .12 .3; ball .5 .12 .16; bunny .74 .12 .42 wow f; spark .52 .4 .07",
                "home; bunny .4 .12 .42 happy; bear .58 .12 .48 happy f; heart .5 .66 .09",
                "home; kite .86 .58 .2 n=0; ball .72 .12 .14; bunny .3 .12 .44 happy; bear .52 .12 .5 happy; heart .41 .68 .08"),

            B("feelings", "F28C3B", 0,
                "meadow; sun .85 .82 .26 happy; fox .35 .12 .44 happy; squirrel .65 .12 .3 happy; heart .5 .62 .08",
                "meadow; sun .82 .8 .3 happy; fox .42 .12 .46 happy; flower .12 .1 .15 c=FFD166; flower .9 .1 .14 c=FF8FAB",
                "rain; fox .45 .12 .46 sad; tuft .82 .1 .1",
                "rain; cloud .5 .82 .3 n=1; fox .4 .12 .46 sad f; tuft .14 .1 .1",
                "rain; fox .5 .12 .46; bubble .6 .6 .04; bubble .64 .68 .05",
                "rain; leaf .62 .5 .62; squirrel .66 .12 .3 happy f; fox .28 .12 .44 wow",
                "rain; leaf .5 .5 .7; fox .38 .12 .44 happy; squirrel .62 .12 .3 happy f; spark .5 .2 .05",
                "night; moon .78 .76 .22 n=4; fox .45 .12 .44 sleep; zz .6 .5 .1",
                "meadow; fox .35 .12 .44 happy; squirrel .62 .12 .3 happy f; heart .5 .62 .08; flower .88 .1 .14 c=BDB2FF"),

            B("rain", "6A9FD4", 0,
                "rain; duck .38 .12 .3 happy; bunny .66 .12 .4 happy f; flower .9 .1 .14 c=FFD166",
                "meadow; cloud .3 .8 .3 n=1; cloud .72 .84 .26 n=1; bunny .5 .12 .44 sad",
                "rain; duck .32 .12 .32 happy; bunny .68 .12 .42 wow f",
                "rain; flower .3 .1 .24 c=FF8FAB; flower .5 .1 .2 c=FFD166; flower .7 .1 .24 c=BDB2FF",
                "pond; cloud .72 .84 .26 n=1; lily .45 .15 .3; frog .45 .14 .34 happy; note .58 .55 .07; note .66 .62 .06",
                "rain; snail .5 .12 .3 happy; tuft .2 .1 .12; tuft .82 .1 .12",
                "rain; bunny .35 .12 .42 happy; duck .65 .12 .3 happy f; spark .5 .2 .06; spark .44 .26 .04",
                "meadow; sun .5 .78 .3 happy; cloud .16 .86 .2; bunny .3 .12 .4 happy; duck .7 .12 .28 happy f",
                "meadow; rainbow .5 .26 1; bunny .32 .12 .42 happy; duck .6 .12 .3 happy f; flower .88 .1 .16 c=FF8FAB"),

            B("balloons", "E86A7A", 0,
                "meadow; bear .36 .12 .5 happy; balloon .58 .38 .3 c=FF8FAB; balloon .66 .42 .34 c=FF6B6E; balloon .74 .46 .36 c=FFD166; balloon .82 .4 .32 c=7FB6E0; balloon .9 .44 .34 c=8FD16A",
                "meadow; bear .3 .12 .5 happy; balloon .52 .36 .36 c=FF6B6E; balloon .6 .4 .38 c=FFD166; balloon .68 .36 .36 c=7FB6E0; balloon .76 .4 .38 c=8FD16A; balloon .84 .36 .36 c=FF8FAB",
                "meadow; bear .36 .12 .5 wow; balloon .6 .38 .34 c=FF6B6E; balloon .68 .42 .36 c=FFD166; balloon .76 .38 .34 c=7FB6E0; balloon .84 .42 .36 c=8FD16A; balloon .92 .38 .34 c=FF8FAB; heart .36 .72 .07",
                "meadow; bear .26 .12 .5 happy; fox .62 .12 .42 happy f; balloon .72 .38 .36 c=FF6B6E",
                "pond; bear .2 .12 .48 happy; duck .6 .18 .26 happy f; balloon .68 .4 .34 c=FFD166",
                "pond; lily .5 .15 .3; frog .5 .14 .34 happy; balloon .62 .34 .38 c=8FD16A; lily .18 .16 .16",
                "meadow; bear .26 .12 .5; mouse .6 .12 .24 happy; balloon .67 .28 .34 c=7FB6E0",
                "meadow; bear .45 .12 .52 happy; balloon .6 .4 .38 c=FF8FAB; heart .4 .72 .07",
                "meadow; fox .14 .12 .38 happy; balloon .24 .44 .3 c=FF6B6E; duck .32 .12 .24 happy; balloon .4 .36 .28 c=FFD166; bear .52 .12 .48 happy; balloon .64 .42 .32 c=FF8FAB; frog .74 .12 .24 happy f; balloon .82 .36 .3 c=8FD16A; mouse .92 .12 .2 happy f; balloon .97 .3 .26 c=7FB6E0"),

            B("goodnight", "2F3E6E", 0,
                "night; moon .78 .76 .24 n=4 happy; hen .3 .12 .36 sleep; dog .6 .12 .36 sleep; zz .72 .45 .1",
                "sunset; hen .26 .12 .38 happy; dog .52 .12 .38 happy; cat .78 .12 .3 happy f",
                "night; moon .8 .78 .22 n=4; hen .45 .12 .42 sleep; zz .58 .5 .1",
                "night; moon .2 .8 .2 n=4; dog .5 .12 .42 sleep; zz .64 .48 .1",
                "night; star .3 .78 .035; star .7 .7 .03; cat .5 .12 .34 sleep; zz .62 .42 .09",
                "night; duck .4 .12 .28 sleep; duck .62 .12 .26 sleep f; zz .52 .45 .08",
                "night; wheat .7 .12 .34 n=3; mouse .45 .12 .24 sleep; zz .54 .34 .07",
                "night; moon .2 .8 .22 n=4 happy; branch .72 .48 .6; owl .74 .5 .26 happy",
                "night; moon .66 .74 .3 n=4 happy; star .3 .8 .04; star .44 .7 .035; star .18 .64 .03; hen .2 .12 .28 sleep; dog .42 .12 .3 sleep; cat .6 .12 .26 sleep; duck .8 .12 .22 sleep f").Calm(),

            B("walk", "8AB66B", 0,
                "meadow; tree .72 .28 .6; mouse .35 .12 .28 happy; mushroom .14 .1 .14; flower .9 .1 .14 c=FFD166",
                "meadow; mouse .4 .12 .3 happy; tuft .7 .1 .12; flower .86 .1 .15 c=FF8FAB",
                "meadow; snail .58 .12 .26 happy; mouse .24 .12 .26 happy",
                "meadow; tree .7 .28 .6; mushroom .5 .1 .18; mushroom .66 .1 .14; mouse .22 .12 .26 happy",
                "meadow; ladybug .42 .12 .14 happy; ladybug .58 .12 .12 happy; ladybug .74 .12 .14 happy f; mouse .2 .12 .26 happy; tuft .9 .1 .1",
                "garden; flower .36 .1 .2 c=FF8FAB; flower .5 .1 .22 c=FFD166; flower .64 .1 .2 c=BDB2FF; flower .78 .1 .22 c=FFADAD; mouse .18 .12 .26 happy",
                "meadow:autumn; acorn .4 .1 .1; acorn .5 .1 .1; acorn .6 .1 .1; acorn .7 .1 .1; acorn .8 .1 .1; mouse .22 .12 .26 happy",
                "garden; butterfly .3 .6 .14 c=FF8FAB; butterfly .46 .72 .13 c=A0C4FF; butterfly .62 .6 .14 c=FFD166; butterfly .78 .74 .13 c=B9FBC0; butterfly .5 .46 .12 c=BDB2FF; butterfly .86 .5 .13 c=FFADAD; mouse .16 .12 .26 wow",
                "sunset; mouse .45 .12 .3 happy; heart .56 .5 .07").Count(null, "snail", "mushroom", "ladybug", "flower", "acorn", "butterfly", null),

            B("nest", "7BC4A8", 1,
                "meadow; tree .5 .26 .7 n=1; nest .5 .62 .16 n=3; bird .62 .7 .1 happy f",
                "meadow; tree .62 .26 .7 n=1; bird .26 .6 .12 happy",
                "meadow; tree .5 .26 .7 n=1; nest .5 .62 .16 n=0; bird .64 .7 .1 happy f",
                "meadow; branch .55 .45 .6; nest .55 .5 .2 n=3; bird .72 .58 .12 happy f",
                "rain; branch .55 .45 .6; nest .55 .5 .2 n=3; bird .55 .56 .12 sleep",
                "meadow; branch .55 .45 .6; nest .55 .5 .2 n=0; bird .49 .56 .07 wow; bird .56 .57 .07 happy; bird .63 .56 .07 wow f; spark .72 .7 .06",
                "meadow; branch .55 .45 .6; nest .55 .5 .2 n=0; bird .49 .57 .08 happy; bird .56 .58 .08 happy; bird .63 .57 .08 happy f; bird .82 .7 .12 happy f",
                "meadow; branch .55 .45 .6; bird .4 .56 .1 happy; bird .52 .58 .1 wow; bird .64 .56 .1 happy f; bird .8 .64 .12 happy f",
                "sky; bird .3 .7 .1 happy; bird .5 .78 .09 happy; bird .68 .68 .1 happy; bird .84 .5 .14 happy f; heart .86 .72 .06"),

            B("shy", "4FA3A5", 0,
                "pond; turtle .38 .14 .3 happy; lily .66 .15 .26; frog .66 .14 .28 happy f",
                "pond; turtle .5 .14 .32; lily .2 .16 .16; lily .82 .17 .16",
                "pond; turtle .3 .14 .3 sad; duck .66 .18 .24 happy f",
                "pond; turtle .3 .14 .3 sad; butterfly .64 .56 .18 happy c=A0C4FF",
                "pond; turtle .35 .14 .3; frog .6 .14 .3 f; lily .82 .16 .2",
                "pond; turtle .35 .14 .3; frog .6 .14 .3 happy f; lily .82 .16 .22",
                "pond; turtle .4 .14 .32 wow; frog .64 .14 .3 happy f; heart .52 .5 .07",
                "pond; lily .45 .15 .36; turtle .38 .15 .24 happy; frog .54 .15 .22 happy f; fish .76 .14 .09 c=FF9F43; fish .88 .2 .08 c=FFD166 f",
                "pond; turtle .35 .14 .3 happy; frog .58 .14 .28 happy f; duck .82 .18 .22 happy f; heart .46 .56 .08"),

            B("turns", "F2A65A", 0,
                "garden; cat .26 .12 .34 happy; ball .5 .12 .14; dog .74 .12 .4 happy f",
                "garden; cat .26 .12 .34 wow; ball .5 .12 .14; dog .74 .12 .4 wow f",
                "garden; cat .24 .12 .34 wow; dog .62 .12 .42 f; ball .78 .12 .13",
                "garden; tree .5 .28 .66; cat .5 .12 .32 sad",
                "garden; dog .3 .12 .42; ball .66 .12 .13",
                "garden; dog .5 .12 .42 sad; ball .7 .12 .13; tuft .2 .1 .1",
                "garden; tree .28 .28 .6; cat .3 .12 .32 sad; dog .64 .12 .42 f; heart .48 .62 .07",
                "garden; cat .22 .12 .34 happy; ball .5 .12 .14; dog .78 .12 .42 happy f",
                "garden; cat .22 .12 .34 happy; ball .5 .12 .14; dog .78 .12 .42 happy f; heart .5 .6 .07; spark .36 .5 .05"),

            B("please", "C9A0DC", 0,
                "home; bread .5 .14 .24; mouse .26 .12 .26 happy; hen .74 .12 .42 happy f; heart .5 .6 .07",
                "home; mouse .5 .12 .3 sad",
                "home; bread .66 .14 .26; mouse .3 .12 .26 wow",
                "home; bread .5 .14 .22; mouse .25 .12 .26; hen .74 .12 .42 sad f",
                "home; mouse .4 .12 .3; spark .48 .52 .06; hen .78 .12 .4 f",
                "home; bread .5 .14 .22; mouse .25 .12 .26 happy; hen .74 .12 .42 wow f",
                "home; bread .44 .14 .2; mouse .25 .12 .26 happy; hen .7 .12 .42 happy f; spark .5 .5 .06",
                "home; mouse .3 .12 .26 happy; bread .38 .14 .14; hen .66 .12 .42 happy f; heart .5 .6 .07",
                "home; mouse .3 .12 .26 happy; hen .65 .12 .42 happy f; heart .46 .6 .08; spark .8 .7 .06"),

            B("autumn", "C8733A", 1,
                "meadow:autumn; tree .7 .26 .66 n=3; hedgehog .35 .12 .34 happy; leaf .52 .14 .1 c=F4A259; acorn .18 .1 .08",
                "meadow:autumn; tree .5 .26 .7 n=3; hedgehog .24 .12 .3 happy; squirrel .8 .12 .28 happy f",
                "meadow:autumn; tree .55 .26 .7 n=3; leaf .3 .6 .1 c=E76F51; leaf .8 .5 .09 c=F6C453; leaf .4 .4 .08 c=F4A259; hedgehog .22 .12 .3 wow",
                "meadow:autumn; tree .75 .26 .62 n=3; hedgehog .4 .12 .34 sleep; zz .52 .44 .08",
                "meadow:autumn; leaf .5 .12 .12 c=E76F51; leaf .62 .12 .1 c=F6C453; leaf .72 .13 .11 c=F4A259; hedgehog .32 .12 .3 happy",
                "meadow:autumn; tree .85 .26 .6 n=3; hedgehog .36 .12 .3 happy; squirrel .64 .12 .3 happy f; leaf .52 .12 .12 c=E76F51",
                "meadow:autumn; leaf .4 .12 .14 c=F6C453; leaf .5 .12 .16 c=F4A259; leaf .6 .12 .14 c=E76F51; hedgehog .24 .12 .3 happy; squirrel .78 .12 .3 happy f; heart .5 .5 .07",
                "sunset; hedgehog .36 .12 .32 sleep; squirrel .66 .12 .3 happy f; leaf .48 .12 .12 c=F6C453",
                "snow; tree .75 .3 .6 n=0; hedgehog .4 .12 .3 sleep; zz .52 .42 .08; leaf .34 .12 .12 c=F4A259; leaf .48 .12 .12 c=E76F51").Calm(),

            B("crab", "2E8BB8", 1,
                "under; crab .4 .1 .24 happy; turtle .68 .5 .3 happy f; bubble .5 .74 .05; fish .2 .66 .14 happy c=FFD166",
                "under; crab .3 .1 .24; fish .55 .7 .14 happy c=FF9F43; fish .78 .62 .12 happy c=A0C4FF f; star5 .82 .1 .18 happy",
                "under; crab .45 .1 .26 wow; fish .62 .72 .12 c=FFD166; bubble .5 .5 .04",
                "under; crab .45 .1 .26 sad; bubble .55 .4 .04; star5 .16 .1 .16",
                "under; crab .35 .1 .24 wow; turtle .64 .34 .32 happy f",
                "under; turtle .5 .4 .34 happy; crab .5 .52 .14 happy; bubble .3 .7 .05; bubble .7 .74 .04",
                "under; turtle .5 .58 .34 happy; crab .5 .7 .14 wow; bubble .3 .8 .05; bubble .7 .84 .04; spark .8 .7 .06; spark .2 .78 .05",
                "under; turtle .5 .5 .3 happy; crab .5 .61 .12 happy; fish .22 .6 .14 happy c=FF9F43; fish .8 .66 .13 happy c=A0C4FF f; fish .74 .44 .12 happy c=FFD166 f",
                "under; crab .45 .1 .26 happy; turtle .72 .45 .3 happy f; fish .22 .6 .14 happy c=FF9F43; heart .45 .5 .07"),

            B("concert", "9BC53D", 0,
                "meadow; cricket .25 .12 .3 happy; frog .48 .12 .26 happy; bird .7 .5 .12 happy f; bee .86 .55 .16 happy; note .4 .6 .07; note .6 .7 .06",
                "meadow; cricket .45 .12 .36 happy; note .58 .55 .07; flower .85 .1 .15 c=FFD166",
                "meadow; cricket .3 .12 .32 happy; frog .62 .12 .3 happy f; note .45 .58 .06; note .74 .6 .06",
                "meadow; tree .76 .28 .62; bird .74 .62 .12 happy f; cricket .22 .12 .3 happy; frog .44 .12 .26 happy; note .62 .76 .06",
                "meadow; bee .7 .55 .2 happy f; cricket .22 .12 .3 happy; frog .42 .12 .26 happy; note .56 .7 .06",
                "pond; duck .62 .18 .28 happy f; cricket .2 .12 .28 happy; frog .38 .12 .24 happy; note .74 .6 .06",
                "meadow; cricket .16 .12 .3 wow; frog .34 .12 .26 wow; duck .54 .12 .26 wow; bee .74 .5 .16 wow f; bird .9 .12 .12 wow f; spark .3 .6 .06; spark .6 .66 .07; spark .8 .74 .05",
                "meadow; cricket .5 .12 .36 happy; frog .26 .12 .26; duck .74 .12 .26 f; heart .5 .64 .07",
                "meadow; cricket .16 .12 .3 happy; frog .32 .12 .26 happy; duck .5 .12 .26 happy; bird .66 .12 .14 happy; bee .84 .45 .18 happy f; note .3 .6 .07; note .5 .7 .06; note .7 .62 .07"),

            B("owl", "5B4E8C", 1,
                "sunset; branch .76 .48 .6; owl .78 .5 .26 happy; bunny .3 .12 .4 happy",
                "meadow; sun .2 .8 .3 happy; branch .76 .48 .6; owl .78 .5 .24 sleep; zz .9 .72 .08; bunny .42 .12 .4 happy",
                "meadow; branch .6 .48 .7; owl .62 .5 .28 sleep; zz .74 .76 .09",
                "meadow; bunny .4 .12 .42 happy; tuft .7 .1 .12; flower .86 .1 .15 c=FFD166",
                "garden; bee .5 .52 .24 happy; flower .26 .1 .24 c=FF8FAB; flower .74 .1 .22 c=BDB2FF",
                "sunset; bunny .35 .12 .42 sleep; zz .46 .45 .08; branch .76 .48 .6; owl .78 .5 .24",
                "night; moon .25 .78 .22 n=4 happy; branch .76 .48 .6; owl .78 .5 .26 wow",
                "night; moon .7 .8 .2 n=4 happy; star .2 .82 .04; star .36 .72 .03; star .5 .86 .035; branch .4 .44 .6; owl .42 .46 .26 happy",
                "night; moon .2 .78 .2 n=4; bunny .35 .12 .38 sleep; branch .76 .48 .6; owl .78 .5 .26 happy f; star .5 .7 .035; star .6 .82 .03").Calm(),

            B("crumbs", "7A9CC6", 1,
                "snow; pine .82 .2 .5 n=1; bear .35 .12 .46 happy; bread .56 .1 .14; bird .64 .14 .1 happy f",
                "snow; pine .2 .2 .5 n=1; pine .82 .22 .44 n=1; tree .52 .3 .62 n=0",
                "snow; tree .7 .3 .66 n=0; bird .72 .62 .1 sad; bird .3 .12 .1 sad",
                "snow; branch .72 .48 .6; bird .74 .52 .12 sad f; bear .3 .12 .46 wow",
                "snow; bear .3 .12 .46 happy; bread .58 .12 .14; spark .66 .2 .04; spark .58 .26 .03",
                "snow; bear .24 .12 .44 happy; bread .56 .1 .12; bird .66 .12 .12 happy f; heart .62 .4 .06",
                "snow; bear .18 .12 .44 happy; bread .5 .1 .12; bird .38 .12 .1 happy; bird .62 .12 .1 happy f; bird .72 .14 .09 happy f; bird .86 .16 .09 happy f",
                "garden; tree .5 .26 .7 n=1; bear .22 .12 .44 happy; flower .84 .1 .16 c=FF8FAB",
                "garden; tree .72 .26 .66 n=1; bird .7 .62 .1 happy f; bird .82 .56 .1 happy f; bear .3 .12 .46 happy; note .6 .74 .06; note .76 .8 .05"),
        };
    }
}
