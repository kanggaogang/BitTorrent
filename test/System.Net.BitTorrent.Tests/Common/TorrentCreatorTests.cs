using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.BitTorrent.Common;
using System.Net.BitTorrent.BEncoding;
using System.Net.BitTorrent.Client;
using System.IO;
using System.Net.BitTorrent.Client.PieceWriters;
using System.Security.Cryptography;
using System.Reflection;

namespace System.Net.BitTorrent.Common
{
    public class TestTorrentCreator : TorrentCreator
    {
        protected override PieceWriter CreateReader()
        {
            TestWriter writer = new TestWriter();
            writer.DontWrite = true;
            return writer;
        }
    }

    public class TorrentCreatorFixture : IDisposable
    {
        public TorrentCreatorFixture()
        {
#if NETSTANDARD1_5
#else
            HashAlgoFactory.Register<SHA1, SHA1Fake>();
#endif
        }

        public void Dispose()
        {
#if NETSTANDARD1_5
#else
            HashAlgoFactory.Register<SHA1, SHA1CryptoServiceProvider>();
#endif
        }
    }

    public class TorrentCreatorTests:IClassFixture<TorrentCreatorFixture>
    {
        private string Comment = "My Comment";
        private string CreatedBy = "Created By MonoTorrent";
        private int PieceLength = 64 * 1024;
        private string Publisher = "My Publisher";
        private string PublisherUrl = "www.mypublisher.com";
        private BEncodedString CustomKey = "Custom Key";
        private BEncodedString CustomValue = "My custom value";

        RawTrackerTiers announces;
        private TorrentCreator creator;
        List<TorrentFile> files;
        TestWriter writer;

        public TorrentCreatorTests()
        {
            creator = new TestTorrentCreator();
            announces = new RawTrackerTiers ();
            announces.Add(new RawTrackerTier (new string[] { "http://tier1.com/announce1", "http://tier1.com/announce2" }));
            announces.Add(new RawTrackerTier (new string[] { "http://tier2.com/announce1", "http://tier2.com/announce2" }));

            creator.Comment = Comment;
            creator.CreatedBy = CreatedBy;
            creator.PieceLength = PieceLength;
            creator.Publisher = Publisher;
            creator.PublisherUrl = PublisherUrl;
            creator.SetCustom(CustomKey, CustomValue);
            files = new List<TorrentFile>(new TorrentFile[] { 
                new TorrentFile(Path.Combine(Path.Combine("Dir1", "SDir1"), "File1"), (int)(PieceLength * 2.30), 0, 1),
                new TorrentFile(Path.Combine(Path.Combine("Dir1", "SDir1"), "File2"), (int)(PieceLength * 36.5), 1, 3),
                new TorrentFile(Path.Combine(Path.Combine("Dir1", "SDir2"), "File3"), (int)(PieceLength * 3.17), 3, 12),
                new TorrentFile(Path.Combine(Path.Combine("Dir2", "SDir1"), "File4"), (int)(PieceLength * 1.22), 12, 15),
                new TorrentFile(Path.Combine(Path.Combine("Dir2", "SDir2"), "File5"), (int)(PieceLength * 6.94), 15, 15),
            });

            writer = new TestWriter();
            writer.DontWrite = true;
        }

        [Fact]
        public void CreateMultiTest()
        {
            foreach (var v in announces)
                creator.Announces.Add (v);

            BEncodedDictionary dict = creator.Create("TorrentName", files);
            Torrent torrent = Torrent.Load(dict);

            VerifyCommonParts(torrent);
            for (int i = 0; i < torrent.Files.Length; i++)
                Assert.True(files.Exists (delegate(TorrentFile f) { return f.Equals(torrent.Files[i]); }));
        }
        [Fact]
        public void NoTrackersTest()
        {
            BEncodedDictionary dict = creator.Create("TorrentName", files);
            Torrent t = Torrent.Load(dict);
            Assert.Equal(0, t.AnnounceUrls.Count);
        }

        [Fact]
        public void CreateSingleTest()
        {
            foreach (var v in announces)
                creator.Announces.Add (v);

            TorrentFile f = new TorrentFile(Path.GetFileName(files[0].Path),
                                            files[0].Length,
                                            files[0].StartPieceIndex,
                                            files[0].EndPieceIndex);

            BEncodedDictionary dict = creator.Create(f.Path, new List<TorrentFile> (new TorrentFile[] { f }));
            Torrent torrent = Torrent.Load(dict);

            VerifyCommonParts(torrent);
            Assert.Equal(1, torrent.Files.Length);
            Assert.Equal(f, torrent.Files[0]);
        }
        [Fact]
        public void CreateSingleFromFolder()
        {
#if NETSTANDARD1_5
            System.Reflection.Assembly assembly = this.GetType().GetTypeInfo().Assembly;
#else
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
#endif
            BEncodedDictionary dict = creator.Create(new TorrentFileSource(assembly.Location));

            Torrent t = Torrent.Load(dict);

            Assert.Equal(1, t.Files.Length);
            Assert.Equal(Path.GetFileName(assembly.Location), t.Name);
            Assert.Equal(Path.GetFileName(assembly.Location), t.Files[0].Path);

            // Create it again
            creator.Create(new TorrentFileSource(assembly.Location));
        }

        [Fact]
        public void LargeMultiTorrent()
        {
            string name1 = Path.Combine(Path.Combine("Dir1", "SDir1"), "File1");
            string name2 = Path.Combine(Path.Combine("Dir1", "SDir1"), "File1");
            string name3 = Path.Combine(Path.Combine("Dir1", "SDir1"), "File1");
            string name4 = Path.Combine(Path.Combine("Dir1", "SDir1"), "File1");
            string name5 = Path.Combine(Path.Combine("Dir1", "SDir1"), "File1");
            files = new List<TorrentFile>(new TorrentFile[] { 
                new TorrentFile(name1, (long)(PieceLength * 200.30), 0, 1),
                new TorrentFile(name2, (long)(PieceLength * 42000.5), 1, 3),
                new TorrentFile(name3, (long)(PieceLength * 300.17), 3, 12),
                new TorrentFile(name4, (long)(PieceLength * 100.22), 12, 15),
                new TorrentFile(name5, (long)(PieceLength * 600.94), 15, 15),
            });

            Torrent torrent = Torrent.Load (creator.Create("BaseDir", files));
            Assert.Equal(5, torrent.Files.Length);
            Assert.Equal(name1, torrent.Files[0].Path);
            Assert.Equal(name2, torrent.Files[1].Path);
            Assert.Equal(name3, torrent.Files[2].Path);
            Assert.Equal(name4, torrent.Files[3].Path);
            Assert.Equal(name5, torrent.Files[4].Path);
        }

        [Fact]
        public void IllegalDestinationPath ()
        {
            Assert.Throws<ArgumentException>(()=> {
                var source = new CustomFileSource(new List<FileMapping> {
                new FileMapping("a", "../../dest1"),
            });
                new TorrentCreator().Create(source);
            });
        }

        [Fact]
        public void TwoFilesSameDestionation ()
        {
            Assert.Throws<ArgumentException>(() => {
                var source = new CustomFileSource(new List<FileMapping> {
                new FileMapping("a", "dest1"),
                new FileMapping ("b", "dest2"),
                new FileMapping ("c", "dest1"),
            });
                new TorrentCreator().Create(source);
            });
        }

        void VerifyCommonParts(Torrent torrent)
        {
            Assert.Equal(Comment, torrent.Comment);
            Assert.Equal(CreatedBy, torrent.CreatedBy);
            Assert.True((DateTime.Now - torrent.CreationDate) < TimeSpan.FromSeconds(5), "#3");
            Assert.Equal(PieceLength, torrent.PieceLength);
            Assert.Equal(Publisher, torrent.Publisher);
            Assert.Equal(PublisherUrl, torrent.PublisherUrl);
            Assert.Equal(2, torrent.AnnounceUrls.Count);
            Assert.Equal(2, torrent.AnnounceUrls[0].Count);
            Assert.Equal(2, torrent.AnnounceUrls[1].Count);
        }
    }
}
