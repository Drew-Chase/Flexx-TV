(() => {
    document.title = `Search ${library.split('')[0].toUpperCase()}${library.substring(1)} - FlexxTV`;
    let searchBar = $("input[type='text']")[0];
    let searchBarArea = $("#search-bar-area")[0];
    let searchView = $("#search-view")[0];

    let timer;

    $(searchBar).on("keyup", () => {
        searchBarArea.classList.add('active')
        window.history.pushState('', '', `${window.location.origin}/search?c=${library}&q=${searchBar.value}`);

        searchView.classList.add('active')
        if (searchBar.value == "") {
            searchView.classList.remove('active')
            searchBarArea.classList.remove('active')
        }
        clearTimeout(timer);
        $("#search-query")[0].innerHTML = `Showing Results for <b>"${searchBar.value}"</b>`;
        timer = setTimeout(() => search(searchBar.value), 1000 * 1)
    })
    $(searchBar).on("focusout", () => {
        if (searchBar.value == "") {
            searchView.classList.remove('active')
            searchBarArea.classList.remove('active')
            $("#search-items")[0].innerHTML = "";
            window.history.pushState('', '', `${window.location.origin}/search?c=${library}`);
        }
    })
    EndLoading();
}).call()
async function search(query) {
    if (query != "") {
        document.title = `Search for "${query}" - FlexxTV`;
    }
    $("#search-items")[0].innerHTML = "";
    $("#search-area")[0].style.background = "transparent";

    let parent = document.createElement('div');
    let throbber = document.createElement('div');
    throbber.classList.add("throbber");
    parent.appendChild(throbber)
    parent.style.position = "absolute";
    parent.style.left = "45vw";
    parent.style.top = "45vh";

    parent.remove();
    $("#search-items")[0].appendChild(parent);

    let url = `${window.location.origin}/api/get/filtered?library=${library}&category=search&query=${query}`
    let response = await fetch(url);
    let results = await response.json();

    if (query != "")
        $("#search-query")[0].innerHTML = `Showing <b>${results.length}</b> Results for <b>"${query}"</b>`;
    parent.remove();

    Array.from(results).forEach(result => {
        let id = result.id;
        let item = document.createElement('div')
        item.classList.add('search-item');

        let poster = document.createElement('img');
        poster.classList.add('poster');
        poster.src = GetImage(id, result.type, "poster")

        let descriptors = document.createElement('div')
        descriptors.classList.add('descriptors');

        let title = document.createElement('div');
        title.innerHTML = result.title;
        title.classList.add('search-item-title');

        let year = document.createElement('div');
        year.innerHTML = `Year: <b>${result.year}</b>`;
        year.classList.add('search-item-extra');

        let mpaa = document.createElement('div');
        mpaa.innerHTML = `MPAA: <b>${result.mpaa == "" || result.mpaa == null ? "NR" : result.mpaa}</b>`;
        mpaa.classList.add('search-item-extra');

        let rating = document.createElement('div');
        rating.innerHTML = `<img class="tmdb-icon" src="/assets/images/tmdb.svg" onerror="this.style.display='none';this.parentElement.innerHTML = 'Score'+this.parentElement.innerText"> <b>${result.rating}</b>/10`;
        rating.classList.add('search-item-extra');

        let description = document.createElement('div');
        description.innerHTML = result.plot;
        description.classList.add('search-item-description');

        let actions = document.createElement('div')
        actions.classList.add('search-actions');

        let addBtn = document.createElement('button');
        addBtn.classList.add("primary");
        if (result.downloaded)
            addBtn.innerHTML = `<i class="fa-solid fa-play"></i> Play`;
        else
            addBtn.innerHTML = `<i class="fa-solid fa-plus"></i> Add`;
        let trailerBtn = document.createElement('button');
        trailerBtn.classList.add("secondary");
        trailerBtn.innerHTML = `<i class="fa-solid fa-film"></i> Watch Trailer`;
        $(trailerBtn).on('click', () => window.location.href = `${window.location.origin}/Watch?library=${library}&id=${result.id}&trailer=true`)
        let moreBtn = document.createElement('button');
        moreBtn.classList.add("secondary");
        moreBtn.innerHTML = `<i class="fa-solid fa-angle-right"></i>`;
        moreBtn.style.padding = ""
        $(moreBtn).on('click', () => window.location.href = `${window.location.origin}/View?library=${result.type}&id=${result.id}`)

        let background = document.createElement('img');
        background.src = GetImage(id, result.type, "cover")
        background.classList.add('background')
        background.onerror = () => background.remove();

        actions.appendChild(addBtn)
        actions.appendChild(trailerBtn)
        actions.appendChild(moreBtn)

        descriptors.appendChild(title)
        descriptors.appendChild(year)
        descriptors.appendChild(mpaa)
        descriptors.appendChild(rating)
        descriptors.appendChild(description)
        descriptors.appendChild(actions)

        item.appendChild(poster)
        item.appendChild(descriptors)
        item.appendChild(background)

        $("#search-items")[0].appendChild(item);
        $("#search-area")[0].style.background = "";
    })
}