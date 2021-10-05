using System;
using System.Collections.Generic;
using Flexx.Core.Data.Exceptions;
using Flexx.Media.Interfaces;
using Flexx.Media.Utilities;

namespace Flexx.Media.Objects.Libraries
{
    public class TvLibraryModel : LibraryModel
    {
        public static TvLibraryModel Instance = Instance ?? new TvLibraryModel();
        public List<TVModel> TVShows { get; private set; }
        protected TvLibraryModel() : base()
        {

        }
        public override void Initialize()
        {
            TVShows = new();
            Scanner.ForTV();
            base.Initialize();
        }
        public TVModel GetTVShowByName(string name)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.Title.Equals(name)) return model;
            }
            throw new SeriesNotFoundException(name);
        }
        public TVModel GetShowByTMDB(string tmdb)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.TMDB.Equals(tmdb)) return model;
            }
            throw new SeriesNotFoundException(tmdb);
        }
        public void AddMedia(params TVModel[] shows)
        {
            TVShows.AddRange(shows);
        }


        public object[] GetList()
        {
            object[] model = new object[TVShows.Count];
            for (int i = 0; i < model.Length; i++)
            {
                if (TVShows[i] != null)
                {
                    TVModel show = TVShows[i];
                    model[i] = new
                    {
                        id = show.TMDB,
                        title = show.Title,
                        year = show.StartDate.Year,
                        seasons = show.Seasons.Count,
                    };

                }
            }
            return model;
        }

    }
}
