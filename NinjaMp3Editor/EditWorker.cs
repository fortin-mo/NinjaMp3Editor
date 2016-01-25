using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NinjaMp3Editor
{
    class EditWorker
    {
        #region Member
        private Lastfm.Services.Session LFMSession;
        private bool _TitleSet = false;
        private bool _AlbumSet = false;
        private bool _GenreSet = false;
        private bool _YearSet = false;
        private bool _ArtistSet = false;
        private int _FileIndex = 0;
        private int _FileCount = -1;
        private Form1 _form;
        #endregion

        #region Properties
        public bool Override { get; set; }
        public string FolderPath { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Genres { get; set; }
        public bool LastFM { get; set; }
        #endregion

        #region Constructor
        public EditWorker()
        {
        }
        #endregion

        #region Editing Methods
        private bool SetTagValues(FileInfo fileInfo, TagLib.File info, bool reset = false)
        {
            ResetTagBool();
            return SetTitleTag(fileInfo, info, reset)
                && SetArtistTag(fileInfo, info, reset)
                && SetAlbumTag(fileInfo, info, reset)
                && SetGenreTag(fileInfo, info, reset)
                && SetYearTag(fileInfo, info, reset);
        }
        private bool SetArtistTag(FileInfo fileInfo, TagLib.File info, bool reset = false)
        {
            //SET ARTIST
            if (info.Tag.AlbumArtists.Count() == 0 || reset)
            {
                var artists = new List<string>();
                try
                {
                    var s = fileInfo.Name.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Count() > 1)
                    {
                        if (s[0].IndexOf(" & ") >= 0)
                        {
                            var s2 = s[0].Split(new string[] { " & " }, StringSplitOptions.RemoveEmptyEntries);
                            artists = s2.ToList();
                        }
                        else
                        {
                            artists.Add(s[0].Trim());
                        }
                        if (LFMSession.Authenticated)
                        {
                            var lastFMArtist = Lastfm.Services.Artist.Search(artists.First(), LFMSession).GetFirstMatch();
                            if (lastFMArtist != null)
                            {
                                var lastFMTrack = Lastfm.Services.Track.Search(lastFMArtist.Name, info.Tag.Title, LFMSession).GetFirstMatch();
                                if (lastFMTrack != null)
                                {
                                    var LastFMAlbum = lastFMTrack.GetAlbum();
                                    if (LastFMAlbum != null)
                                    {
                                        info.Tag.Album = LastFMAlbum.Name;
                                        _AlbumSet = true;
                                        //if (LastFMAlbum.GetReleaseDate() != null)
                                        //{
                                        //    info.Tag.Year = (uint)LastFMAlbum.GetReleaseDate().Year;
                                        //    _YearSet = true;
                                        //}
                                    }
                                }
                            }
                        }
                    }
                    else if (_TitleSet && LFMSession.Authenticated)
                    {
                        var lastFMTrack = Lastfm.Services.Track.Search(info.Tag.Title, LFMSession).GetFirstMatch();
                        if (lastFMTrack != null)
                        {
                            artists.Add(lastFMTrack.Artist.Name);
                            var LastFMAlbum = lastFMTrack.GetAlbum();
                            if (LastFMAlbum != null)
                            {
                                info.Tag.Album = LastFMAlbum.Name;
                                _AlbumSet = true;
                                //if (LastFMAlbum.GetReleaseDate() != null) 
                                //{
                                //    info.Tag.Year = (uint)LastFMAlbum.GetReleaseDate().Year;
                                //    _YearSet = true;
                                //}
                            }
                        }
                    }

                    _ArtistSet = artists.Count() > 0;
                    info.Tag.AlbumArtists = artists.ToArray();
                    WriteToConsole(fileInfo.Name + " -> changing artists to " + artists.First());
                }
                catch (Exception)
                {
                    throw new Exception("could not get the artist from -> " + fileInfo.Name);
                }
            }
            return true;
        }
        private bool SetAlbumTag(FileInfo fileInfo, TagLib.File info, bool reset = false)
        {
            //SET ALBUM
            if ((string.IsNullOrEmpty(info.Tag.Album) && !string.IsNullOrEmpty(this.Album) && !_AlbumSet) || (reset && !_AlbumSet))
            {
                WriteToConsole(fileInfo + " -> changing album to " + this.Album);
                info.Tag.Album = this.Album;
            }
            return true;
        }
        private bool SetGenreTag(FileInfo fileInfo, TagLib.File info, bool reset = false)
        {
            //SET GENRES
            if ((info.Tag.Genres.Count() == 0 && !string.IsNullOrEmpty(this.Genres)) || reset)
            {
                try
                {
                    var genres = new List<string>();

                    var s = this.Genres.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < s.Length; i++)
                    {
                        genres.Add(s[i].Trim());
                    }

                    WriteToConsole("changing genres to " + genres.First());

                    info.Tag.Genres = genres.ToArray();

                }
                catch (Exception)
                {
                    throw new Exception("Wrong genres formating !");
                }
            }
            return true;
        }
        private bool SetTitleTag(FileInfo fileInfo, TagLib.File info, bool reset = false)
        {
            //SET TITLE
            if (string.IsNullOrEmpty(info.Tag.Title) || reset)
            {
                try
                {
                    var title = "";
                    var s = fileInfo.Name.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

                    var titleIndex = s.Count() > 1 ? 1 : 0;
                    title = s[titleIndex].Remove(s[titleIndex].Length - fileInfo.Extension.Length).Trim();
                    var start = title.IndexOf("(");
                    var end = title.LastIndexOf(")");
                    if (start > 0)
                    {
                        Console.WriteLine(title);
                        if (end > 0 && end > start) title = title.Remove(start, end - start + 1);
                        else title = title.Remove(start);
                    }
                    WriteToConsole(fileInfo.Name + " -> changing title to " + title);
                    _TitleSet = true;
                    info.Tag.Title = title.Trim();
                }
                catch (Exception)
                {
                    throw new Exception("could not get the title from -> " + fileInfo.Name);
                }
            }
            return true;
        }
        private bool SetYearTag(FileInfo fileInfo, TagLib.File info, bool reset = false)
        {
            //SET YEAR
            if (((info.Tag.Year == null || info.Tag.Year == 0) && !string.IsNullOrEmpty(this.Year) && !_YearSet) || (reset && !_YearSet))
            {
                WriteToConsole(fileInfo + " -> changing year to " + this.Year);
                info.Tag.Year = uint.Parse(this.Year);
            }
            return true;
        }

        #endregion

        #region helpder
        private bool ConnectLastFM()
        {
            if (this.LastFM)
            {
                try
                {
                    LFMSession = new Lastfm.Services.Session("3cb49ab221079da39a6fda69aa5ae556", "f93a2d205121f53a8b332f17061ba98f");
                    LFMSession.Authenticate("mofortin", "c8906de3b634c32e5a1000cb2a5f6377");
                }
                catch (Exception e)
                {
                    MessageBox.Show("ERROR : " + e.Message);
                    return false;
                }
            }
            else
            {
                LFMSession = new Lastfm.Services.Session("", "");
            }
            return true;
        }

        private void WriteToConsole(string text)
        {
            OnLogChanged(new LogChangedArgs(text));
        }

        private void ResetTagBool()
        {
            _TitleSet = false;
            _AlbumSet = false;
            _GenreSet = false;
            _YearSet = false;
            _ArtistSet = false;
        }

        #endregion

        #region Thread

        // This method will be called when the thread is started.
        public void DoWork()
        {
            _shouldStop = false;

            if (_FileIndex > 0) OnProcessResumed(new EventArgs());
            else OnProcessStarted(new EventArgs());
            
            while (!_shouldStop)
            {
                var reset = this.Override;

                if (Directory.Exists(this.FolderPath))
                {
                    ConnectLastFM();
                    WriteToConsole("Starting ....");
                    var files = Directory.GetFiles(this.FolderPath);
                    if (_FileCount >= 0 && files.Count() != _FileCount)
                    {
                        _shouldStop = true;
                        WriteToConsole("ERROR : Changes append to the selected directorry, we cannot resume the progress. Please cancel and restart !");
                        return;
                    }
                    _FileCount = files.Count();

                    for (int i = _FileIndex; i < _FileCount; i++)
                    {
                        if (_shouldStop) break;
                        var file = files[i];
                        if (File.Exists(file))
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Extension == ".mp3")
                            {
                                try
                                {
                                    WriteToConsole("Editing file -> " + fileInfo.Name);
                                    TagLib.File info = TagLib.File.Create(file);
                                    SetTagValues(fileInfo, info, reset);
                                    //SAVE
                                    info.Save();
                                    WriteToConsole("SUCCESS : " + fileInfo.Name + " was saved !");
                                }
                                catch (Exception ex)
                                {
                                    WriteToConsole("ERROR : " + ex.Message);
                                    WriteToConsole("ERROR : could not save file -> " + fileInfo.Name);
                                }
                            }
                        }
                        _FileIndex = i;
                        OnProgressChanged(new ProgressChangedArgs(_FileIndex + 1, _FileCount));
                    }
                    OnProcessFinished(new EventArgs());
                    _FileIndex = 0;
                    _shouldStop = true;
                }
                else
                {
                    _shouldStop = true;
                    WriteToConsole("ERROR : This folder doesn't exist !");
                    return;
                }
            }
        }

        public void DoClear()
        {
            _shouldStop = false;
            OnProcessStarted(new EventArgs());
            while (!_shouldStop)
            {
                if (Directory.Exists(this.FolderPath))
                {
                    WriteToConsole("Starting ....");
                    var files = Directory.GetFiles(this.FolderPath);
                    if (_FileCount >= 0 && files.Count() != _FileCount)
                    {
                        _shouldStop = true;
                        WriteToConsole("ERROR : Changes append to the selected directorry, we cannot resume the progress. Please cancel and restart !");
                        return;
                    }
                    _FileCount = files.Count();
                    
                    for (int i = _FileIndex; i < _FileCount; i++)
                    {
                        var file = files[i];
                        if (File.Exists(file))
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Extension == ".mp3")
                            {
                                try
                                {
                                    WriteToConsole("Clearing properties -> " + fileInfo.Name);
                                    TagLib.File info = TagLib.File.Create(file);
                                    info.Tag.AlbumArtists = new string[] { };
                                    info.Tag.Album = "";
                                    info.Tag.Title = "";
                                    info.Tag.Genres = new string[] { };
                                    info.Tag.Year = 0;

                                    info.Save();
                                    WriteToConsole("SUCCESS : " + fileInfo.Name + " was saved !");
                                }
                                catch (Exception ex)
                                {
                                    WriteToConsole("ERROR : " + ex.Message);
                                    WriteToConsole("ERROR : could not save file -> " + fileInfo.Name);
                                }
                            }
                        }
                        _FileIndex = i;
                        OnProgressChanged(new ProgressChangedArgs(_FileIndex + 1, _FileCount));
                    }
                    OnProcessFinished(new EventArgs());
                    _FileIndex = 0;
                    _shouldStop = true;
                }
                else
                {
                    _shouldStop = true;
                    WriteToConsole("ERROR : This folder doesn't exist !");
                    return;
                }
            }
        }

        public void RequestPause()
        {
            _shouldStop = true;
            OnProcessPaused(new EventArgs());
        }

        public void RequestCancel()
        {
            _shouldStop = true;
            _FileCount = -1;
            _FileIndex = 0;
            ResetTagBool();
            OnProcessFinished(new EventArgs());
        }

        // Volatile is used as hint to the compiler that this data
        // member will be accessed by multiple threads.
        private volatile bool _shouldStop;

        #endregion

        #region Event
        public event EventHandler ProcessFinished;
        protected void OnProcessFinished(EventArgs e)
        {
            if (ProcessFinished != null)
            {
                ProcessFinished(this, e);
            }
        }

        public event EventHandler ProcessResumed;
        protected void OnProcessResumed(EventArgs e)
        {
            if (ProcessResumed != null)
            {
                ProcessResumed(this, e);
            }
        }

        public event EventHandler ProcessPaused;
        protected void OnProcessPaused(EventArgs e)
        {
            if (ProcessPaused != null)
            {
                ProcessPaused(this, e);
            }
        }

        public event EventHandler ProcessStarted;
        protected void OnProcessStarted(EventArgs e)
        {
            if (ProcessStarted != null)
            {
                ProcessStarted(this, e);
            }
        }

        public event EventHandler<ProgressChangedArgs> ProgressChanged;
        protected void OnProgressChanged(ProgressChangedArgs e)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, e);
            }
        }

        public event EventHandler<LogChangedArgs> LogChanged;
        protected void OnLogChanged(LogChangedArgs e)
        {
            if (LogChanged != null)
            {
                LogChanged(this, e);
            }
        }
        #endregion
    }

    class ProgressChangedArgs : EventArgs
    {
        public int Index { get; private set; }
        public int Count { get; private set; }
        public ProgressChangedArgs(int index, int count)
        {
            Index = index;
            Count = count;
        }
    }

    class LogChangedArgs : EventArgs
    {
        public string Log { get; private set; }

        public LogChangedArgs(string log)
        {
            Log = log;
        }
    }
}
