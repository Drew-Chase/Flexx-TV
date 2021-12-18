let pages = $(".pages")[0];
$(".fs-input").on('click', e => {
    if (!e.currentTarget.classList.contains('active')) {
        if (e.currentTarget.dataset.cd != null && e.currentTarget.dataset.cd != "")
            LoadFS(e.currentTarget, e.currentTarget.dataset.cd)
        else
            LoadFS(e.currentTarget)
    }
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

$("#previous").on("click", e => {
    if (pages.scrollLeft != 0) {
        pages.scrollBy(-(pages.offsetWidth), 0)
    }
})
$("#next").on("click", e => {
    $("#previous")[0].disabled = false
    if ($("#next")[0].innerText == "Finish") {
        finish();
    } else {
        if (pages.scrollLeft + pages.offsetWidth != pages.scrollWidth) {
            pages.scrollBy(pages.offsetWidth, 0)
        }
    }
})
$(".login-form input").on('keyup', e => {
    if (e.key == "enter") {
        $(".login-form button")[0].click();
    }
})
$(".login-form button").on("click", async () => {
    let email = $(`.login-form input[type="email"]`)[0].value;
    let password = $(`.login-form input[type="password"]`)[0].value;
    let formData = new FormData();
    formData.append("username", email)
    formData.append("password", password)
    let response = await fetch(`/login`, { method: "POST", body: formData });
    let json = await response.json();
    let error = $(".login-form .error")[0];
    if (json.hasOwnProperty("error")) {
        error.innerHTML = json["error"];
    } else if (json.hasOwnProperty("token")) {
        $(`.login-form`)[0].dataset.token = json["token"];
        $(`.login-form`)[0].parentElement.parentElement.parentElement.classList.remove('active');
    } else {
        error.innerHTML = "Either Email/Username or Password was incorrect"
    }
})

async function LoadFS(e, d = "") {
    let data = new FormData();
    data.append("dir", d)
    let response = await fetch("/fs", { method: 'POST', body: data })
    let json = await response.json();
    e.dataset.cd = json.cd;
    let fs = e.querySelector(".fs")
    let icon = e.querySelector(".cta-icon")
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

async function finish() {
    let parent = document.createElement('div');
    parent.id = "loading";
    let throbber = document.createElement('div');
    throbber.classList.add("throbber");
    parent.appendChild(throbber)
    $("body")[0].appendChild(parent)
    $("body")[0].style.overflow = "hidden"
    let data = new FormData();
    data.append("movie", $("#movie-card")[0].dataset.cd)
    data.append("tv", $("#tv-card")[0].dataset.cd)
    data.append("portForward", $("#portForward")[0].dataset.checked)
    data.append("port", $("#server-port")[0].value)
    data.append("token", $(".login-form")[0].dataset.token)
    await fetch("/finish", { method: 'POST', body: data }).catch(e => {
        let load = setInterval(async () => {
            if ((await fetch(`${window.location.protocol}//${window.location.hostname}:${$("#server-port")[0].value}/api/status`)).status == 200) {
                clearInterval(load);
                window.location.href = `${window.location.protocol}//${window.location.hostname}:${$("#server-port")[0].value}`;
            }
        }, 5 * 1000);
    })
}

setInterval(() => {
    if (pages.scrollLeft == 0) {
        $("#previous")[0].disabled = true
    } else {
        $("#previous")[0].disabled = false
    }

    if (pages.scrollLeft + pages.offsetWidth == pages.scrollWidth) {
        if ($("#movie-card")[0].dataset.cd != null && $("#tv-card")[0].dataset.cd != null && $(".login-form")[0].dataset.token != null) {
            $("#next")[0].innerText = "Finish"
            $("#next")[0].disabled = false;
        }
        else
            $("#next")[0].disabled = true;
    } else {
        $("#next")[0].disabled = false;
        $("#next")[0].innerText = "Next"
    }
}, 50)