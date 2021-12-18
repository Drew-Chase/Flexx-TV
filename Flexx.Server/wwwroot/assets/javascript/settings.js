if (window.location.pathname == "/settings") {
    let settings = [
        content = {
            name: "Content",
            items: [
                {
                    text: "Dashboard",
                    active: true,
                },
                {
                    text: "Library",
                },
                {
                    text: "Customizations",
                },
            ]
        },

        server = {
            name: "Server",
            items: [
                {
                    text: "Networking",
                },
                {
                    text: "Connections",
                },
                {
                    text: "Transcoder",
                },
            ]
        }
    ]

    let sidebar = document.createElement('div');
    sidebar.classList.add('sidebar')

    let title = document.createElement('p')
    title.id = "sidebar-title";
    title.innerText = "Settings"
    sidebar.appendChild(title)
    let sidebarItems = document.createElement("div");
    sidebarItems.classList.add('sidebar-items');
    Array.from(settings).forEach(setting => {
        let subtitle = document.createElement("p")
        subtitle.innerText = setting.name;
        subtitle.classList.add('sidebar-subtitle')
        sidebarItems.appendChild(subtitle);
        Array.from(setting.items).forEach(item => {
            let button = document.createElement('div')
            button.classList.add("sidebar-item");
            button.innerText = item.text;
            $(button).on('click', e => {
                $("#view").load(`/settings/page?page=${item.text.toLowerCase()}`, () => {
                    Init();
                })
                Array.from(sidebarItems.children).forEach(c => c.classList.remove('active'))
                e.target.classList.add('active')
                window.history.replaceState('', ``, `?c=${item.text.toLowerCase()}`)
            })

            if (item.active != null && item.active && page == "") {
                button.click();
            }
            if (item.text.toLowerCase() == page.toLowerCase()) {
                button.click();
            }
            sidebarItems.appendChild(button);
        })
    })
    sidebar.appendChild(sidebarItems)
    $("#view")[0].before(sidebar)
}
EndLoading();
async function LoadNowPlaying() {
    let url = `${window.location.origin}/api/stream/get/active`
    let response = await fetch(url);
    let json = await response.json();

    let list = $("#now-playing-list")[0]
    Array.from(json).forEach(async item => {
        let username = "Drew Chase";
        let id = `card_${item.id}_${item.resolution.name}_${username}`.replaceAll(" ", "");

        let card = document.createElement('div');
        card.id = id;
        card.classList.add('nowplaying-card');

        let description = document.createElement('div');
        description.classList.add('description');

        let mediaImage = document.createElement('img');
        mediaImage.src = `${window.location.origin}/api/get/images?library=${item.type.toLowerCase()}&id=${item.id}&type=poster`;
        $(mediaImage).on('error', () => mediaImage.remove());

        let extras = document.createElement('div')
        extras.classList.add('extra');

        let information = await (await fetch(`${window.location.origin}/api/get/info?id=${item.id}&library=${item.type.toLowerCase()}${item.episode != -1 && item.season != -1 ? "&season=" + item.season : "&episode=" + item.episode}`)).json()

        let title = document.createElement('div')
        title.classList.add('title')
        title.innerText = information.title;

        let year = document.createElement('div')
        year.classList.add('year')
        year.innerText = information.year;

        let time = document.createElement('div')
        time.classList.add('remainingTime')
        time.innerText = GetStringTimeFromSeconds(item.currentPosition);

        extras.appendChild(title)
        extras.appendChild(year)
        extras.appendChild(time)
        description.appendChild(mediaImage);
        description.appendChild(extras);

        let advancedInformation = document.createElement('div')
        advancedInformation.classList.add('advanced-information')

        let clientIcon = document.createElement('div')
        clientIcon.classList.add('client-icon')
        clientIcon.innerHTML = GetBrowserIcon(item.platform)

        let info = document.createElement('div')
        info.classList.add('info')

        let client = document.createElement('div')
        client.classList.add('client')
        client.innerText = `${item.platform}`;

        let state = document.createElement('div')
        state.classList.add('state')
        state.innerText = `${item.state}`

        let quality = document.createElement('div')
        quality.classList.add('quality')
        quality.innerText = `${item.resolution.name} - ${item.resolution.bitRate}`

        info.appendChild(client)
        info.appendChild(state)
        info.appendChild(quality);
        advancedInformation.appendChild(clientIcon)
        advancedInformation.appendChild(info)

        let user = document.createElement('div');
        user.classList.add('user');

        let userImage = document.createElement('img')
        userImage.src = `http://flexx-tv.tk/assets/images/multicolor-profile-picture-square-sm-320x320.jpeg`

        let name = document.createElement('p')
        name.classList.add('name')
        name.innerText = username
        let action = document.createElement('div')
        action.classList.add('action');
        action.innerText = "Stop Playback"
        $(action).on('click', () => {
            alert('This is not yet implemented')
        })
        user.appendChild(userImage)
        user.appendChild(name)
        user.appendChild(action)

        card.appendChild(description)
        card.appendChild(advancedInformation)
        card.appendChild(user)
        if ($(`#${id}`).length == 0) {
            list.appendChild(card);
        } else {
            $(`#${id} .state`)[0].innerText = `${item.state}`
            $(`#${id} .remainingTime`)[0].innerText = GetStringTimeFromSeconds(item.currentPosition);
        }
    })

    Array.from($('[id*="card_"')).forEach(card => {
        let found = false;
        Array.from(json).forEach(async item => {
            let username = "Drew Chase";
            let id = `card_${item.id}_${item.resolution.name}_${username}`.replaceAll(" ", "");
            if (id == card.id) found = true;
        })
        if (!found) {
            card.remove();
        }
    })

    setTimeout(() => LoadNowPlaying(), 1000)
}

function LoadEvents() {
    $(".fs-input").on('click', e => {
        if (!e.currentTarget.classList.contains('active')) {
            if (e.currentTarget.dataset.cd != null && e.currentTarget.dataset.cd != "")
                LoadFS(e.currentTarget, e.currentTarget.dataset.cd)
            else
                LoadFS(e.currentTarget)
        }
    })
    Array.from($(".fs-input .cta-text")).forEach(e => {
        e.innerText = e.parentElement.parentElement.parentElement.dataset.cd
    })
    $(".fs-apply").on('click', e => {
        setTimeout(() => {
            e.currentTarget.parentElement.parentElement.parentElement.classList.remove('active');
        }, 50)
    })
    $(".card:not(.toggle):not(.noflip)").on('click', e => {
        Array.from($(".card")).forEach(item => item.classList.remove('active'))
        e.currentTarget.classList.add('active')
    })

    $(".card.toggle").on('click', e => {
        e.currentTarget.classList.toggle('active')
        e.currentTarget.dataset.checked = !e.currentTarget.dataset.checked;
    })
}

async function LoadFS(e, d = "") {
    let data = new FormData();
    data.append("dir", d)
    let response = await fetch("/settings/fs", { method: 'POST', body: data })
    let json = await response.json();
    e.dataset.cd = json.cd;
    let fs = e.querySelector(".fs")
    let icon = e.querySelector(".cta-icon")
    if (icon != null)
        icon.innerHTML = `<i class="fa-regular fa-circle-check"></i>`
    let input = fs.parentElement.querySelector("input");
    input.value = json.cd;
    $(input).on('focusout', () => LoadFS(e, input.value));
    $(input.parentElement.querySelector('.fs-cd-apply')).on('click', () => LoadFS(e, input.value));
    fs.innerHTML = "";
    let back = document.createElement("div");
    back.classList.add("directory");
    back.innerText = "..";
    fs.appendChild(back)
    $(back).on('click', () => LoadFS(e, json.parent))
    Array.from(json.directories).forEach(item => {
        let directory = document.createElement("div");
        directory.classList.add("directory");
        directory.innerText = item;
        fs.appendChild(directory)
        $(directory).on('click', () => LoadFS(e, d + (d.endsWith('/') ? "" : "/") + item));
    })
}
function GetStringTimeFromSeconds(seconds) {
    let totalHours = Math.floor(seconds / 60 / 60);
    let totalMinutes = Math.floor(seconds / 60) - (totalHours * 60);
    let totalSeconds = Math.floor(seconds % 60);

    totalHours = (totalHours > 9 ? totalHours : "0" + totalHours);
    totalMinutes = totalMinutes > 9 ? totalMinutes : "0" + totalMinutes;
    totalSeconds = totalSeconds > 9 ? totalSeconds : "0" + totalSeconds;
    let time = "";
    if (totalHours != 00)
        time += totalHours + "h "
    if (totalMinutes != 00)
        time += totalMinutes + "m "
    return `${time} ${totalSeconds}s`;
}