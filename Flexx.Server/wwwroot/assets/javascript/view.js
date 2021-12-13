let view;
(async () => {
    view = await GetView();

    if (Object.keys(view).length === 0) window.location.href = `/${lib}`

    let castElement = $("#cast .carousel-items")[0]
    castElement.innerHTML = "";
    Array.from(view.mainCast).forEach(cast => {
        let member = document.createElement('div');
        member.classList.add('cast-member');
        member.classList.add('banner');
        member.classList.add('circular');
        try {
            if (cast["profileImage"] != null)
                member.style.backgroundImage = `linear-gradient(transparent, black), url('${cast.profileImage}')`;
            let name = document.createElement('div')
            name.innerText = cast.name;
            name.classList.add('cast-member-name')
            let role = document.createElement('div')
            role.innerText = cast.role;
            role.classList.add('cast-member-role')
            member.appendChild(name)
            member.appendChild(role)
            castElement.append(member)
        } catch {
            member.remove()
        }

        $(member).on('click', () => {
            window.location.href = `${window.location.origin}/search?c=actors&q=${cast.name}`
        })
    })

    let play = $("#play")[0];
    $("#banner-poster")[0].style.backgroundImage = `url("${GetImage(id, lib, "poster")}")`;
    $("#banner-poster")[0].dataset.watched = view.watched;
    $("#banner-poster")[0].dataset.watched_percentage = view.watchedPercentage == null ? 0 : view.watchedPercentage;
    if ($("#banner-poster")[0].dataset.watched_percentage != 0) {
        percentageBar = document.createElement("span")
        percentageBar.classList.add("watched_percentage")
        percentageBar.style.width = `calc(${$("#banner-poster")[0].dataset.watched_percentage}% - 20px * 2)`
        $("#banner-poster")[0].appendChild(percentageBar)
    }
    $(".banner-background")[0].style.backgroundImage = `url("${GetImage(id, lib, "cover")}")`;
    $(".banner-title")[0].innerHTML = `<img src="${GetImage(id, lib, "logo")}" alt="${view.title}" onerror="this.parentElement.innerHTML = '${view.title}'"/>`;
    document.title = `${view.title} - FlexxTV`;
    $(".banner-description")[0].innerText = view.plot;
    if (view.rating != null)
        $(".view-rating")[0].innerHTML = `${view.rating > 10 ? view.rating / 10 : view.rating}/10`;
    else
        $(".view-rating")[0].remove();
    if (view.mpaa != null && view.mpaa != '')
        $(".view-mpaa")[0].innerHTML = view.mpaa;
    else
        $(".view-mpaa")[0].remove();
    if (view.fullDuration != null && view.fullDuration != '')
        $(".view-length")[0].innerHTML = view.fullDuration;
    else
        $(".view-length")[0].remove();
    LoadSimilar(view.id);

    if (lib == "tv") {
        play.innerHTML = `${(view.added ? `<i class="fa-solid fa-play"></i> Play - ${view.upNext.name}` : `<i class="fa-solid fa-plus"></i> Add`)}`;
        if (season == null && episode == null) {
            $(play).on('click', () => {
                if (view.added) {
                    window.location.href = `${window.location.origin}/Watch?library=tv&episode=${view.upNext.episode}&season=${view.upNext.season}&id=${id}`;
                }
            })
            let items = $(".library-items")[0];
            Array.from((await GetSeasons()).seasons).forEach(season => {
                let parent = document.createElement("div");
                parent.classList.add("library-item")
                parent.classList.add("tv-banner")
                parent.dataset.watched = season.watched == null ? false : season.watched;
                parent.dataset.watched = season.watched == null ? false : season.watched;
                parent.dataset.watched_percentage = 0;

                let title = document.createElement('div')
                title.classList.add("library-item-title")
                title.innerHTML = season.name;

                let subtitle = document.createElement('div')
                subtitle.classList.add("library-item-subtitle")
                subtitle.innerHTML = `${season.episodes} Episodes`;

                let background = document.createElement('img')
                background.classList.add("library-item-background")
                background.src = GetImage(id, lib, "poster", false, season.season)

                // Events
                $(parent).on('click', () => window.location.href = `${window.location.origin}/View?library=tv&id=${id}&season=${season.season}`)

                parent.appendChild(title)
                title.appendChild(subtitle)
                parent.appendChild(background)
                items.appendChild(parent)

                let context = {
                    title: season.name,
                    items:
                        [
                            {
                                text: "Add",
                                action: () => { }
                            },
                            {
                                text: `Mark ${season.name} as Watched`,
                                action: () => alert('Not implemented yet')
                            }
                        ]
                }
                $(parent).contextmenu(() => {
                    if (season.downloaded)
                        context.items[0] =
                        {
                            text: `Watch ${season.upNext.name}`,
                            action: () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}&season=${season.upNext.number}`
                        }
                    openMenu(context)
                })
            })

            let context = {
                title: view.title,
                items:
                    [
                        {
                            text: "Add",
                            action: () => { }
                        },
                        {
                            text: "Mark Show as Watched",
                            action: () => alert('Not implemented yet')
                        }
                    ]
            }
            if (view.added) {
                context.items[0] =
                {
                    text: `Watch ${view.upNext.name}`,
                    action: () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}`
                };
            }

            $("#view-banner").contextmenu(() => {
                openMenu(context)
            })
            $(".additional-options").on('click', () => {
                openMenu(context)
            })
        } else if (season != null && episode == null) {
            let url = `${window.location.origin}/api/get/info?library=${lib}&id=${id}&username=${user}&season=${season}`;
            let response = await fetch(url);
            view = await response.json();

            if (Object.keys(view).length === 0) window.location.href = `${window.location.origin}/View?library=${lib}&id=${id}`

            if (view.upNext != null)
                $("#play")[0].innerHTML = `Play ${view.upNext.name}`;
            else
                $("#play")[0].innerHTML = `Add`;
            document.title = `${view.show} - FlexxTV`;
            let context = {
                title: view.name,
                items:
                    [
                        {
                            text: "Watch",
                            action: () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}`
                        },
                        {
                            text: "Mark as Watched",
                            action: () => alert('Not implemented yet')
                        },
                        {
                            text: "Back to Show",
                            action: () => window.location.href = `${window.location.origin}/View?library=tv&id=${id}`
                        }
                    ]
            }
            $("#banner-poster")[0].style.backgroundImage = `url("${GetImage(id, lib, "poster")}")`;
            $(".banner-background")[0].style.backgroundImage = `url("${GetImage(id, lib, "cover")}")`;
            $(".banner-title")[0].innerText = view.name;
            $(".banner-description")[0].innerText = view.plot;

            url = `${window.location.origin}/api/get/?library=${lib}&id=${id}&username=${user}&season=${season}`;
            response = await fetch(url);
            view = await response.json();
            let episodes = $("#episodes")[0];
            Array.from(view.episodes).forEach(episode => {
                let parent = document.createElement("div");
                parent.classList.add("banner")
                parent.classList.add("lg")
                parent.classList.add("tv-banner")

                if (!episode.downloaded) {
                    parent.classList.add("ghost")
                } else {
                    parent.dataset.watched = episode.watched
                    parent.dataset.watched_duration = episode.watchedDuration == null ? 0 : episode.watchedDuration;
                    parent.dataset.watched_percentage = episode.watchedPercentage == null ? 0 : episode.watchedPercentage;
                    if (parent.dataset.watched_percentage != 0) {
                        percentageBar = document.createElement("span")
                        percentageBar.classList.add("watched_percentage")
                        percentageBar.style.width = `calc(${parent.dataset.watched_percentage}% - 20px * 2)`
                        parent.appendChild(percentageBar)
                    }
                }

                let title = document.createElement('div');
                title.classList.add("banner-title");
                title.innerHTML = episode.title;

                let subtitle = document.createElement('div')
                subtitle.innerHTML = episode.name
                subtitle.classList.add("banner-subtitle");

                let image = document.createElement('img');
                image.classList.add("banner-background")
                image.classList.add("episode-background")
                image.src = GetImage(id, lib, "poster", false, season, episode.episode)

                // Events
                $(parent).on('click', () => window.location.href = `${window.location.origin}/View?library=tv&id=${id}&season=${season}&episode=${episode.episode}`)

                title.appendChild(subtitle)
                parent.appendChild(title)
                parent.appendChild(image)

                episodes.appendChild(parent)
                let context = {
                    title: episode.name,
                    items:
                        [
                            {
                                text: "Watch",
                                action: () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}`
                            },
                            {
                                text: "Mark as Watched",
                                action: () => alert('Not implemented yet')
                            },
                            {
                                text: "Back to Show",
                                action: () => window.location.href = `${window.location.origin}/View?library=tv&id=${id}`
                            }
                        ]
                }
                if (view.added)
                    context.items[0] = {
                        text: "Add",
                        action: () => { }
                    }
                $(parent).contextmenu(() => {
                    openMenu(context)
                })
            })
            if ($("#play")[0].innerHTML.includes("Add"))
                context.items[0] = {
                    text: "Add",
                    action: () => { }
                }
            $("#view-banner").contextmenu(() => openMenu(context))
            $(".additional-options").on('click', () => {
                openMenu(context)
            })
        } else if (season != null && episode != null) {
            let url = `${window.location.origin}/api/get/info?library=${lib}&id=${id}&username=${user}&season=${season}&episode=${episode}`;
            let response = await fetch(url);
            let view = await response.json();
            if (Object.keys(view).length === 0) window.location.href = `${window.location.origin}/View?library=${lib}&id=${id}&season=${season}`
            let watched_duration = view.watchedDuration == null ? 0 : view.watchedDuration;
            document.title = `${view.show} - FlexxTV`;
            $("#banner-poster")[0].style.backgroundImage = `url("${GetImage(id, lib, "poster", false, season, episode)}")`;
            $(".banner-background")[0].style.backgroundImage = `url("${GetImage(id, lib, "cover")}")`;
            $("#banner-poster")[0].dataset.watched_duration = watched_duration;
            $("#banner-poster")[0].dataset.watched_percentage = view.watchedPercentage == null ? 0 : view.watchedPercentage;
            if ($("#banner-poster")[0].dataset.watched_percentage != 0) {
                percentageBar = document.createElement("span")
                percentageBar.classList.add("watched_percentage")
                percentageBar.style.width = `calc(${$("#banner-poster")[0].dataset.watched_percentage}% - 20px * 2)`
                $("#banner-poster")[0].appendChild(percentageBar)
            }
            $("#banner-poster")[0].style.aspectRatio = "3/2";
            $(".banner-title")[0].innerText = `${view.title} - ${view.name}`;
            $(".banner-description")[0].innerText = view.plot;
            $(".additional-options")[0].dataset.added = view.downloaded;
            $(".additional-options")[0].dataset.library = "tv";
            $(".additional-options")[0].dataset.id = id;

            let n = { disabled: view.nextEpisode == null };
            if (view.nextEpisode != null) {
                n =
                {
                    text: "Next Episode",
                    action: () => window.location.href = `${window.location.origin}/View?library=tv&id=${id}&season=${view.nextEpisode.season}&episode=${view.nextEpisode.episode}`
                }
            }
            let context = {
                items:
                    [
                        {
                            text: "Watch",
                            action: () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}&season=${season}&episode=${episode}&resume=true`
                        },
                        {
                            text: "Watch from Beginning",
                            action: () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}&season=${season}&episode=${episode}`,
                            disabled: watched_duration == 0
                        },
                        n,
                        {
                            text: "Back to Season",
                            action: () => window.location.href = `${window.location.origin}/View?library=tv&id=${id}&season=${season}`
                        },
                        {
                            text: "Back to Show",
                            action: () => window.location.href = `${window.location.origin}/View?library=tv&id=${id}`
                        }
                    ]
            }
            if (view.downloaded) {
                if (watched_duration == 0) {
                    $("#play")[0].innerHTML = `<i class="fa-solid fa-play"></i> Play`;
                    $(play).on("click", () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}&season=${season}&episode=${episode}`)
                } else {
                    $("#play")[0].innerHTML = `<i class="fa-solid fa-play"></i> Resume`;
                    $(play).on("click", () => window.location.href = `${window.location.origin}/Watch?library=tv&id=${id}&season=${season}&episode=${episode}&resume`)
                    context.items[0].text = "Resume"
                }
            } else {
                $("#play")[0].innerHTML = `<i class="fa-solid fa-plus"></i> Add`;
                context.items[0] = {
                    text: "Add",
                    action: () => { }
                }
            }
            $(".banner").contextmenu(() => {
                openMenu(context)
            })
            $(".additional-options").on('click', () => {
                openMenu(context)
            })
        }
    } else if (lib == "movie") {
        let context = {};
        if (view.downloaded) {
            context = {
                title: view.title,
                items:
                    [
                        {
                            text: `Watch from Beginning`,
                            action: () => window.location.href = `${window.location.origin}/Watch?library=movie&id=${id}`
                        },
                        {
                            text: `Watch Trailer`,
                            action: () => window.location.href = `${window.location.origin}/Watch?library=movie&id=${id}&trailer=true`
                        },
                        {
                            text: `Mark as Watched`,
                            action: () => alert('Not implemented yet')
                        }
                    ]
            }
            if (view.watchedDuration == 0) {
                play.innerHTML = `<i class="fa-solid fa-play"></i> Play`;
                $(play).on("click", () => window.location.href = `${window.location.origin}/Watch?library=movie&id=${id}`)
            } else {
                $(play).on("click", () => window.location.href = `${window.location.origin}/Watch?library=movie&id=${id}&resume=true`)
                play.innerHTML = `<i class="fa-solid fa-play"></i> Resume - ${Math.ceil(view.watchedDuration / 60)} Minutes`;
            }
        } else {
            context = {
                title: view.title,
                items:
                    [
                        {
                            text: `Add "${view.title}"`,
                            action: () => alert('Not implemented yet')
                        },
                        {
                            text: `Watch Trailer`,
                            action: () => window.location.href = `${window.location.origin}/Watch?library=movie&id=${id}&trailer=true`
                        }
                    ]
            }
            play.innerHTML = `<i class="fa-solid fa-plus"></i> Add`;
        }

        $("#view-banner").contextmenu(() => openMenu(context));
        $(".additional-options").on('click', () => openMenu(context));
    }
    EndLoading();
}).call();

async function LoadSimilar(id) {
    let car = $("#discover-similar .carousel-items")[0];
    car.parentElement.style.display = "none"
    let url = `${window.location.origin}/api/get/filtered?library=${lib}&category=similar&id=${id}&username=${user}`;
    let response = await fetch(url);
    let releases = await response.json();
    Array.from(releases).forEach(item => createElement(car, item, true, lib == "movie"))
    car.parentElement.style.display = ""
    return releases;
}
async function GetSeasons() {
    let url = `${window.location.origin}/api/get?library=${lib}&id=${id}&username=${user}`;
    let response = await fetch(url);
    let releases = await response.json();
    return releases;
}
async function GetView() {
    let url = `${window.location.origin}/api/get/info?library=${lib}&id=${id}&username=${user}`;
    let response = await fetch(url);
    let releases = await response.json();
    return releases;
}

Array.from($(".cast-left")).forEach(left => {
    $(left).on('click', () => {
        let scroll = left.parentElement.querySelector('.members')
        if (scroll.scrollLeft != 0)
            scroll.scrollBy(-scroll.offsetWidth, 0)
        else
            scroll.scrollBy(scroll.scrollWidth, 0)
    })
})
Array.from($(".cast-right")).forEach(right => {
    $(right).on('click', () => {
        let scroll = right.parentElement.querySelector('.members')
        if (scroll.scrollLeft + scroll.offsetWidth == scroll.scrollWidth) {
            scroll.scrollBy(-scroll.scrollWidth, 0)
        }
        else
            scroll.scrollBy(scroll.offsetWidth, 0)
    })
})