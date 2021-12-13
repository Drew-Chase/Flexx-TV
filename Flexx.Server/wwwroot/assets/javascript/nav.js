(() => {
    BeginLoading();
    var nav = $("nav")[0];
    document.addEventListener("scroll", e => {
        if (screen.width > 800) {
            if (e.target.scrollingElement.scrollTop <= 10) {
                nav.classList.remove("collapsed");
                nav.classList.add("expanded");
            } else {
                nav.classList.remove("expanded");
                nav.classList.add("collapsed");
            }
        }
    })
    Update();
    setInterval(() => Update(), 500)
    function Update() {
        if (screen.width <= 800 || window.location.pathname == "/settings") {
            nav.classList.remove("expanded");
            nav.classList.add("collapsed");
        } else {
            if (document.scrollingElement.scrollTop <= 10) {
                nav.classList.remove("collapsed");
                nav.classList.add("expanded");
            } else {
                nav.classList.remove("expanded");
                nav.classList.add("collapsed");
            }
        }
    }
    $("#nav-home").on("click", () => window.location.href = `/`)
    $("#nav-movie").on("click", () => window.location.href = `${window.location.origin}/Movies`)
    $("#nav-tv").on("click", () => window.location.href = `${window.location.origin}/TV`)
    $("#search-nav-item").on("click", () => window.location.href = `${window.location.origin}/Search`);
    $("#account-nav-item").on("click", () => {
        openMenu(
            {
                title: `Hi, ${user}`,
                items: [
                    {
                        text: "Switch Users",
                        action: () => alert('Not Implemented Yet')
                    },
                    {
                        text: "Account Settings",
                        action: () => window.location.href = `${window.location.origin}/account`
                    },
                    {
                        text: "Server Settings",
                        action: () => window.location.href = `${window.location.origin}/settings`
                    },
                    {
                        text: "Sign Out",
                        action: () => alert('Not Implemented Yet')
                    },
                ]
            }
        )
    });
}).call();
function BeginLoading() {
    let parent = document.createElement('div');
    parent.id = "loading";
    let throbber = document.createElement('div');
    throbber.classList.add("throbber");
    parent.appendChild(throbber)
    $("body")[0].appendChild(parent)
    $("body")[0].style.overflow = "hidden"
}

function EndLoading() {
    let parent = $("#loading")[0];
    parent.classList.add("out");
    $("body")[0].style.overflow = ""
    setTimeout(() => parent.remove(), 1000);

    Array.from($(".carousel-left")).forEach(left => {
        $(left).on('click', () => {
            let scroll = left.parentElement.querySelector('.carousel-items')
            if (scroll.scrollLeft != 0)
                scroll.scrollBy(-(scroll.offsetWidth / 2), 0)
            else
                scroll.scrollBy(scroll.scrollWidth, 0)
        })
    })
    Array.from($(".carousel-right")).forEach(right => {
        $(right).on('click', () => {
            let scroll = right.parentElement.querySelector('.carousel-items')
            if (scroll.scrollLeft + scroll.offsetWidth == scroll.scrollWidth) {
                scroll.scrollBy(-scroll.scrollWidth, 0)
            }
            else
                scroll.scrollBy(scroll.offsetWidth / 2, 0)
        })
    })
}