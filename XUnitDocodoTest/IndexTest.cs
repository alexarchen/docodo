using System;
using Xunit;
using Docodo;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace XUnitDocodoTest
{

    

    public class IndexTest
    {
         const string TestText1 = "Another game, with a similar result, was followed by a revoke from the unlucky Miller;" +
            " on which the fat gentleman burst into a state of high personal excitement which lasted until the conclusion of the game, when he retired into a corner, and remained perfectly mute for one hour and twenty�seven minutes; " +
            "at the end of which time he emerged from his retirement, and offered Mr. Pickwick a pinch of snuff with the air of a man who had made up his mind to a Christian forgiveness of injuries sustained. " +
            "The old lady�s hearing decidedly improved and the unlucky Miller felt as much out of his element as a dolphin in a sentry�box. " +
            "Meanwhile the round game proceeded right merrily.Isabella Wardle and Mr.Trundle �went partners,� and Emily Wardle and Mr.Snodgrass did the same; " +
            "and even Mr.Tupman and the spinster aunt established a joint�stock company of fish and flattery.Old Mr.Wardle was in the very height of his jollity; " +
            "and he was so funny in his management of the board, and the old ladies were so sharp after their winnings, that the whole table was in a perpetual roar of merriment and laughter.There was one old lady who always had about half a dozen cards to pay for, at which everybody laughed, regularly every round; " +
            "and when the old lady looked cross at having to pay, they laughed louder than ever; on which the old lady�s face gradually brightened up, " +
            "till at last she laughed louder than any of them, Then, when the spinster aunt got �matrimony,� the young ladies laughed afresh, and the " +
            "Spinster aunt seemed disposed to be pettish; till, feeling Mr.Tupman squeezing her hand under the table, she brightened up too, and looked rather knowing," +
            " as if matrimony in reality were not quite so far off as some people thought for; whereupon everybody laughed again, and especially old Mr.Wardle, " +
            "who enjoyed a joke as much as the youngest.As to Mr. Snodgrass, he did nothing but whisper poetical sentiments into his partner�s ear, which made one " +
            "old gentleman facetiously sly, about partnerships at cards and partnerships for life, and caused the aforesaid old gentleman to make some remarks thereupon," +
            " accompanied with divers winks and chuckles, which made the company very merry and the old gentleman�s wife especially so. " +
            "And Mr. Winkle came out with jokes which are very well known in town, but are not all known in the country; and as everybody laughed at them very " +
            "heartily, and said they were very capital, Mr.Winkle was in a state of great honour and glory.And the benevolent clergyman looked pleasantly on; for " +
            "the happy faces which surrounded the table made the good old man feel happy too; " +
            "and though the merriment was rather boisterous, still it came from the heart and not from the lips; and this is the right sort of merriment, after all.";
         const string TestHeaders1 = "Size=190\nSource=Test\nTitle=Charles Diskense Pickwick Club\n";

        class TestDataSource : IIndexDataSource
        {


            public string Name { get; set; } = "Test";

            virtual public void Reset() { }
            private int hasNext = 0;
            object readlock = new object();

            virtual public IIndexDocument Next(bool wait)
            {
                if (hasNext<2)
                {
                    lock (readlock)
                    {
                        string name = (hasNext == 0) ? "Sample" : "Dump";
                        IndexPagedTextFile pf = new IndexPagedTextFile(name, TestText1, TestHeaders1+$"Name={name}\n");
                        hasNext++;
                        pf.pages.Add(new IndexPage("2", TestText1));
                        return pf;
                    }
                }
                return null;

            }
            public void Dispose() { }

        }
        Index index = new Index();

        public IndexTest() {
            index.AddDataSource(new TestDataSource());
            index.Create().Wait();
        }

        [Fact]
        public void CoordTest()
        {
            Assert.True(index.CanSearch);

            string[] words = { "and", "tupman", "everybody", "old" };

            // coordinate test
            foreach (string word in words)
            {

                List<int> pos = new List<int>();
                foreach (Match m in Regex.Matches(TestText1.ToLower(), @"\b"+word+@"\b"))
                {
                    pos.Add(m.Index);
                }
                Index.SearchResult res = index.Search(word);

                Assert.Equal(pos.Count, res.foundPages[0].pos.Count);
                foreach (var p in res.foundPages)
                  Assert.True(Enumerable.SequenceEqual(pos, p.pos));

            }

            

        }

        [Fact]
        public void RequestSyntaxTest()
        {
            Assert.True(index.CanSearch);

            Index.SearchResult res = index.Search("and (tupman|old) {Name=Dump}" /*, new Index.SearchOptions(){ dist = 20}*/);

            Assert.True(1==res.foundDocs.Count);

            Assert.True(2==res.foundDocs.First().pages.Count);

            Assert.Equal(res.foundPages[0].pos.Count,res.foundPages[1].pos.Count);
            Assert.Equal(42,res.foundPages[0].pos.Count);
            Assert.True(Enumerable.SequenceEqual(res.foundPages[0].pos, res.foundPages[1].pos));

            res = index.Search("and tupman old");

        }


    }

}