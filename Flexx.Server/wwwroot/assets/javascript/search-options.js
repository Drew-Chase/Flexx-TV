(async () => {
    let movie = $("#movies-search-option .background")[0]
    let tv = $("#tv-search-option .background")[0]
    let popMovies = await GetPopularMovieReleases()
    let popMovie = popMovies[Math.floor(Math.random() * popMovies.length)]
    movie.style.backgroundImage = `url('${GetImage(popMovie.id, "movie", "poster")}')`

    let popTvs = await GetPopularTvReleases()
    let popTv = popTvs[Math.floor(Math.random() * popTvs.length)]
    tv.style.backgroundImage = `url('${GetImage(popTv.id, "tv", "poster")}')`

    $("#actors-search-option .background")[0].style.backgroundImage = `url('${popMovie.mainCast[0].profileImage}')`

    EndLoading();

    $("#movies-search-option").on('click', () => window.location.href = `${window.location.origin}/search?c=movie`);
    $("#tv-search-option").on('click', () => window.location.href = `${window.location.origin}/search?c=tv`);
    $("#actors-search-option").on('click', () => window.location.href = `${window.location.origin}/search?c=actors`);
    async function GetPopularTvReleases() {
        const response = await fetch(`${window.location.origin}/api/get/filtered?category=popular&library=tv&username=${user}`);
        const releases = await response.json();
        return releases;
    }
    async function GetPopularMovieReleases() {
        const response = await fetch(`${window.location.origin}/api/get/filtered?category=popular&library=movie&username=${user}`);
        const releases = await response.json();
        return releases;
    }
}).call();