let elementUpdateTimer = null;

// Dashboard
async function loadFeaturedDashboard(type = -1) {
    let startType = type;
    $("#featured-banner")[0].style.minHeight = `${window.outerHeight - window.outerHeight / 4}px`
    if (type != 1 && type != 2) {
        if (Math.random() <= .5) {
            type = 1;
        } else {
            type = 2;
        }
    }

    let popular = await GetPopularReleases(type == 1);
    let release = popular[Math.floor(Math.random() * popular.length)];
    let title = $("#featured-banner .banner-title")[0];
    let playButton = $("#featured-banner .primary")[0];
    let optionsButton = $("#featured-banner .additional-options")[0];
    let bannerDescription = $("#featured-banner .banner-description")[0];
    let bannerBackground = $("#featured-banner .banner-background")[0];
    try {
        title.innerHTML = `<img src="${GetImage(release.id, type == 1 ? "movie" : "tv", "logo")}" alt="${release.title}" onerror="this.parentElement.innerHTML = '${release.title}'"/>`;
        bannerDescription.innerHTML = release.plot
        playButton.innerHTML = release.downloaded ? `<i class="fa-solid fa-play"></i> Play` : `<i class="fa-solid fa-plus"></i> Add`
        optionsButton.innerHTML = `<i class="fa-solid fa-ellipsis"></i>`
        bannerBackground.style.backgroundImage = `url('${GetImage(release.id, type == 1 ? "movie" : "tv", "cover")}')`

        let items = {
            title: "Options",
            items:
                [
                    {
                        text: `Add "${release.title}"`,
                        action: () => alert('Not implemented yet'),
                        disabled: release.added
                    },
                    {
                        text: `Watch "${release.title}"`,
                        action: () => window.location.href = `${window.location.origin}/Watch?library=${type == 1 ? "movie" : "tv"}&id=${release.id}&resume=true`,
                        disabled: !release.added
                    },
                    {
                        text: `Watch from the Beginning`,
                        action: () => window.location.href = `${window.location.origin}/Watch?library=${type == 1 ? "movie" : "tv"}&id=${release.id}`,
                        disabled: !release.added || release.watchedDuration != 0
                    },
                    {
                        text: `Watch Trailer`,
                        action: () => window.location.href = `${window.location.origin}/Watch?library=movie&id=${release.id}&trailer=true`
                    },
                    {
                        text: `View "${release.title}"`,
                        action: () => window.location.href = `${window.location.origin}/View?library=${type == 1 ? "movie" : "tv"}&id=${release.id}`
                    },
                    {
                        text: "Mark as Watched",
                        action: () => alert('Not implemented yet')
                    }
                ]
        };
        if (type == 2) {
            items.items.splice(1, 1)
        }
        $(optionsButton).on("click", () => openMenu(items))
        $("#featured-banner").contextmenu(() => openMenu(items))
    } catch {
        loadFeaturedDashboard(type);
    }
    let progress = document.createElement('div');
    progress.id = "progress-bar"
    $("#featured-banner")[0].appendChild(progress)
    let index = 0;
    let seconds = 0;
    let maxTime = 30
    let time = setInterval(() => {
        progress.style.maxWidth = `${index}%`;
        if (seconds > maxTime) {
            progress.remove();
            clearInterval(time)
            loadFeaturedDashboard(startType)
        }
        index += 100 / maxTime;
        seconds++;
    }, 1000);
}

async function loadSections(type = 0, update = false) {
    if (type != 1 && type != 2) type = 0;
    let sections = [];
    if (type == 0) {
        sections = [
            {
                name: "Continue Watching TV",
                json: async () => await GetContinueWatching(false),
                movie: false
            },
            {
                name: "Recently Added TV",
                json: async () => await GetRecentlyAdded(false),
                movie: false
            },
            {
                name: "New Shows",
                json: async () => await GetLatestReleases(false),
                movie: false
            },
            {
                name: "Popular Shows",
                json: async () => await GetPopularReleases(false),
                movie: false
            },
            {
                name: "Top Rated Shows",
                json: async () => await GetTopRatedReleases(false),
                movie: false
            },

            {
                name: "Continue Watching Movies",
                json: async () => await GetContinueWatching(true),
                movie: true
            },
            {
                name: "Recently Added Movies",
                json: async () => await GetRecentlyAdded(true),
                movie: true
            },
            {
                name: "New Movies",
                json: async () => await GetLatestReleases(true),
                movie: true
            },
            {
                name: "Popular Movies",
                json: async () => await GetPopularReleases(true),
                movie: true
            },
            {
                name: "Top Rated Movies",
                json: async () => await GetTopRatedReleases(true),
                movie: true
            },
        ]
    } else if (type == 1) {
        sections = [
            {
                name: "Continue Watching Movies",
                json: async () => await GetContinueWatching(true),
                movie: true
            },
            {
                name: "Recently Added Movies",
                json: async () => await GetRecentlyAdded(true),
                movie: true
            },
        ]
    } else if (type == 2) {
        sections = [
            {
                name: "Continue Watching Episodes",
                json: async () => await GetContinueWatching(false),
                movie: false
            },
            {
                name: "Recently Added Episodes",
                json: async () => await GetRecentlyAdded(false),
                movie: false
            },
        ]
    }
    let parent = $("#dash-items")[0];
    sections.forEach(async item => {
        let exists = false;
        if (update) {
            for (var c in Array.from($(".carousel-title"))) {
                if (c.innerText == item.name) {
                    exists = true;
                    break;
                }
            }
        }
        if ((await item.json()).length != 0) {
            let carousel = document.createElement('div');
            carousel.classList.add("carousel")
            let title = document.createElement('div');
            title.classList.add("carousel-title");
            title.innerHTML = item.name;
            let items = document.createElement('div');
            items.classList.add("carousel-items");
            let left = document.createElement('div')
            left.classList.add('carousel-control')
            left.classList.add('carousel-left')
            left.innerHTML = `<i class="fa-solid fa-angle-left"></i>`;
            let right = document.createElement('div')
            right.classList.add('carousel-control')
            right.classList.add('carousel-right')
            right.innerHTML = `<i class="fa-solid fa-angle-right"></i>`;
            Array.from(await item.json()).forEach(j => {
                createElement(items, j, true, item.movie)
            })

            $(left).on('click', () => {
                if (items.scrollLeft != 0)
                    items.scrollBy(-(items.offsetWidth / 2), 0)
                else
                    items.scrollBy(items.scrollWidth, 0)
            })
            $(right).on('click', () => {
                if (items.scrollLeft + items.offsetWidth == items.scrollWidth) {
                    items.scrollBy(-items.scrollWidth, 0)
                }
                else
                    items.scrollBy(items.offsetWidth / 2, 0)
            })

            carousel.appendChild(title)
            carousel.appendChild(items)
            carousel.appendChild(left)
            carousel.appendChild(right)

            parent.prepend(carousel)
        }
    })
    if (type != 0) {
        let library = document.createElement('div')
        library.classList.add("library")
        let title = document.createElement('div')
        title.classList.add("library-title")
        title.innerHTML = type == 1 ? "Movies" : "TV Shows"
        let items = document.createElement('div')
        items.classList.add("library-items")

        Array.from((await GetLibrary(type == 1))).forEach(i => {
            createElement(items, i, false, type == 1)
        })

        library.appendChild(title)
        library.appendChild(items)
        parent.appendChild(library);
    }
    if (!update)
        UpdateElementInfo()
}

function createElement(element, json, horizontal, movie) {
    let lib = movie ? "movie" : "tv"
    let parent = document.createElement('div');
    parent.classList.add("library-item")
    if (horizontal) {
        parent.classList.add("banner")
        parent.classList.add("lg")
        parent.classList.add(`${lib}-banner`)
    }
    parent.dataset.horizontal = horizontal;
    parent.dataset.type = lib;
    let title = document.createElement('div')
    if (horizontal)
        title.classList.add('banner-title')
    else
        title.classList.add('library-item-title')
    title.innerHTML = json.title;

    let image = document.createElement("img")
    image.style.opacity = "0";
    image.style.background = "hsl(0,0%,4%)"
    $(image).on('load', () => image.style.opacity = "")
    if (horizontal)
        image.classList.add("banner-background")
    else
        image.classList.add("library-item-background")

    image.src = `${GetImage(json.id, lib, horizontal ? "cover" : "poster", horizontal)}`;

    // image.onerror(e=>e.target.parentElement.remove())
    parent.appendChild(image)
    parent.appendChild(title)
    element.appendChild(parent);

    // Events
    if (horizontal && movie) {
        let timer;
        let video = document.createElement('video');
        $(parent).on('mouseenter', () => {
            timer = setTimeout(() => {
                video.src = `${window.location.origin}/api/get/stream/trailer?id=${json.id}`;
                video.style.zIndex = "1";
                video.autoplay = true;
                setTimeout(() => video.style.opacity = "1", 250);
                parent.appendChild(video)
            }, 1000)
        })
        $(parent).on('mouseleave', () => {
            clearTimeout(timer);
            video.remove();
        })
    }
    $(parent).on("click", () => {
        window.location.href = `${window.location.origin}/View?library=${lib}&id=${json.id}`;
    })
    parent.dataset.id = json.id;
    parent.dataset.added = json.downloaded == null || json.downloaded == "undefined" ? json.added : json.downloaded;
    parent.dataset.watched = json.watched == null ? false : json.watched;
    parent.dataset.watched_duration = json.watchedDuration == null ? 0 : json.watchedDuration;
    parent.dataset.watched_percentage = json.watchedPercentage == null ? 0 : json.watchedPercentage;
    if (!movie) {
    }
    if (parent.dataset.watched_percentage != 0) {
        percentageBar = parent.querySelectorAll('span.watched_percentage').length == 0 ? document.createElement("span") : parent.querySelector('span.watched_percentage');
        percentageBar.classList.add("watched_percentage")
        percentageBar.style.width = `calc(${parent.dataset.watched_percentage}% - 20px * 2)`
        if (parent.querySelectorAll('span.watched_percentage').length == 0)
            parent.appendChild(percentageBar)
    }
    $(parent).contextmenu(() => {
        let items = {
            items:
                [
                    {
                        text: `Add "${json.title}"`,
                        action: () => alert('Not implemented yet'),
                        disabled: !json.added
                    },
                    {
                        text: `View "${json.title}"`,
                        action: () => window.location.href = `${window.location.origin}/View?library=${lib}&id=${json.id}`
                    },
                    {
                        text: `Watch "${json.title}"`,
                        action: () => window.location.href = `${window.location.origin}/Watch?library=${movie ? "movie" : "tv"}&id=${json.id}&resume=true`,
                        disabled: json.added
                    },
                    {
                        text: `Watch from the Beginning`,
                        action: () => window.location.href = `${window.location.origin}/Watch?library=${movie ? "movie" : "tv"}&id=${json.id}=true`,
                        disabled: json.added || json.watchedDuration == 0
                    },
                    {
                        text: `Watch Trailer`,
                        action: () => window.location.href = `${window.location.origin}/Watch?library=movie&id=${json.id}&trailer=true`,
                        disabled: !movie
                    },
                    {
                        text: "Mark as Watched",
                        action: () => {
                            $.post(`${window.location.origin}/api/post/watched`, { id: json.id, username: user, watched: true, library: movie ? "movie" : "tv" })

                            // parent.dataset.watched = true;
                            closeMenu()
                        },
                        disabled: json.watched
                    },
                    {
                        text: "Mark as Unwatched",
                        action: () => {
                            $.post(`${window.location.origin}/api/post/watched`, { id: json.id, username: user, watched: false, library: movie ? "movie" : "tv" })

                            // parent.dataset.watched = false;
                            closeMenu()
                        },
                        disabled: !json.watched
                    },
                ]
        };
        openMenu(items)
    })
}

async function UpdateElementInfo() {
    if (elementUpdateTimer != null)
        clearTimeout(elementUpdateTimer);
    Array.from($(".library-item")).forEach(async item => {
        if (item.dataset.added != null && item.dataset.added != "undefined" && item.dataset.added) {
            let url = `${window.location.origin}/api/get/info?id=${item.dataset.id}&library=${item.dataset.type}&username=${user}`;
            let response = await fetch(url);
            let json = await response.json();
            item.dataset.added = json.downloaded == null || json.downloaded == "undefined" ? json.added : json.downloaded;
            item.dataset.watched = json.watched == null ? false : json.watched;
            item.dataset.watched_duration = json.watchedDuration == null ? 0 : json.watchedDuration;
            item.dataset.watched_percentage = json.watchedPercentage == null ? 0 : json.watchedPercentage;

            if (item.dataset.watched_percentage != 0) {
                percentageBar = item.querySelectorAll('span.watched_percentage').length == 0 ? document.createElement("span") : item.querySelector('span.watched_percentage');
                percentageBar.classList.add("watched_percentage")
                percentageBar.style.width = `calc(${item.dataset.watched_percentage}% - 20px * 2)`
                if (item.querySelectorAll('span.watched_percentage').length == 0)
                    item.appendChild(percentageBar)
            }
        }
    })

    //elementUpdateTimer = setTimeout(() => UpdateElementInfo(), 5 * 1000);
}

function GetImage(id, library = "movie", type = "cover", language = false, season = -1, episode = -1) {
    return `${window.location.origin}/api/get/images?library=${library}&id=${id}&type=${type}&language=${language}${season == -1 ? "" : `&season=${season}`}${episode == -1 ? "" : `&episode=${episode}`}`;
}

async function GetContinueWatching(movies) {
    return await (await (await fetch(`${window.location.origin}/api/get/filtered?category=continue-watching&username=${user}&library=${movies ? "movie" : "tv"}`)).json());
}
async function GetPopularReleases(movies) {
    const response = await fetch(`${window.location.origin}/api/get/filtered?category=popular&library=${movies ? "movie" : "tv"}&username=${user}`);
    const releases = await response.json();
    return releases;
}
async function GetTopRatedReleases(movies) {
    const response = await fetch(`${window.location.origin}/api/get/filtered?category=top_rated&username=${user}&library=${movies ? "movie" : "tv"}`);
    const releases = await response.json();
    return releases;
}
async function GetLatestReleases(movies) {
    const response = await fetch(`${window.location.origin}/api/get/filtered?category=latest&username=${user}&library=${movies ? "movie" : "tv"}`);
    const releases = await response.json();
    return releases;
}

async function GetRecentlyAdded(movies) {
    return await (await (await fetch(`${window.location.origin}/api/get/filtered?category=recently-added&library=${movies ? "movie" : "tv"}&username=${user}`)).json())
}

async function GetLibrary(movies) {
    return await (await (await fetch(`${window.location.origin}/api/get?username=${user}&library=${movies ? "movie" : "tv"}`)).json());
}