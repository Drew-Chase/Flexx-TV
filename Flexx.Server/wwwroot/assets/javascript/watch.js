// (async () => {
// Initialize Video Element Variables
let player;
let video = document.createElement('video');
let interface = $("#video-controls")[0]
let title = $("#video-title")[0];
let large_play_button = $("#large-play-button")[0];
let play_button = $("#play-button")[0];
let clickable_area = $("#clickable-area")[0];
let progress_bar = $("#progress-bar")[0];
let nextButton = $("#skip-forward-button")[0];
let previousButton = $("#skip-backward-button")[0];

// Instantiate Video Settings
let isFullscreen = false;
let hoveringOnVideoProgress = false;
let draggingVideoProgress = false;
let draggingAudioProgress = false;
let interfaceTimer;
let json;
let videoDuration = 9999999;
let playTimeOffset = 0;
let durationWithOffset = 0;
let activeQualitySetting = "";
let activeAudioSetting = "None";
let streamInfo = {};
let gainNode;
let volume = 1;

// Initialize Network Settings
let updateBufferInterval = 50
let throbber = document.createElement('div')
let lastPlayPos = 0
let currentPlayPos = 0
let bufferingDetected = false
let streamStartTick = 0;

// Initializing Menus
let optionsMenu = {
    title: "Options",
    items:
        [
            {
                text: `Change Quality`,
                action: () => openMenu(qualityOptionsMenu)
            },
            {
                text: `Audio Boost`,
                action: () => openMenu(audioBoostOptionsMenu)
            },
        ]
}
let qualityOptionsMenu = {
    title: "Change Quality",
    items: []
}

let audioBoostOptions = ["None", "Low", "Medium", "High", "Extreme"]

let audioBoostOptionsMenu = {
    title: "Audio Boost",
    items: []
}

// Entry Point
$(document).ready(Init);

// Initialize Page Elements
async function Init() {
    if (season == -1) season = null;
    if (episode == -1) episode = null;
    json = await (await fetch(`${window.location.origin}/api/get/info?library=${library}&id=${id}&username=${user}${season != null && episode != null ? `&season=${season}&episode=${episode}` : ""}`)).json();

    if (json.nextEpisode == null) {
        nextButton.remove();
    } else {
        $(nextButton).on("click", () => {
            season = json.nextEpisode.season;
            episode = json.nextEpisode.episode;
            Init();
        })
    }
    if (json.previousEpisode == null) {
        previousButton.remove();
    }

    // Video Player Settings
    video.id = "flexx-player";
    video.autoplay = true;

    let startPos = resume ? json.watchedDuration : 0;

    if (!isTrailer) {
        $("main")[0].appendChild(video);
        let quality = Array.from(json.versions)[0].displayName;
        activeQualitySetting = quality;
        videojs(video, {
            controls: false,
            autoplay: true,
            preload: 'auto',
        }).ready(function () {
            player = this;
            SetVideoSource(startPos)
        })
        await PingServer()
    } else {
        video.autoplay = true
        video.src = `${window.location.origin}/api/stream/trailer?id=${id}`;
        $("main")[0].appendChild(video);
    }
    video.style.background = "black";

    // Document Settings
    if (library == "movie") {
        title.innerHTML = `${json.title} (${json.year})${isTrailer ? ` - Trailer` : ""}`;
    } else {
        title.innerHTML = `${json.show}<br><div class="player-subtitle">${json.name} - ${json.title}</div>`;
    }
    document.title = `Watching ${title.innerText} - FlexxTV`;

    // Throbber Settings
    throbber.classList.add("throbber")
    throbber.style.position = "absolute"
    throbber.style.top = `calc(50% - 50px)`
    $("main")[0].appendChild(throbber)
    InitEvents();
    InitAudioBooster();
    InitMenus();
}

function InitAudioBooster() {
    let audioCtx = new AudioContext();
    let source = audioCtx.createMediaElementSource(video);

    // create a gain node
    gainNode = audioCtx.createGain();
    gainNode.gain.value = 1; // double the volume
    source.connect(gainNode);

    // connect the gain node to an output destination
    gainNode.connect(audioCtx.destination);
}

function InitMenus() {

    // Menu Settings
    qualityOptionsMenu.items = [];
    if (!isTrailer) {
        Array.from(json.versions).forEach(version => {
            qualityOptionsMenu.items.push({
                text: `${version.displayName} ${version.displayName == activeQualitySetting ? "<b>(ACTIVE)</b>" : ""}`,
                action: () => {
                    ChangeQualitySetting(version.displayName)
                    closeMenu();
                }
            })
        })
    }
    audioBoostOptionsMenu.items = [];
    for (let i = 0; i < audioBoostOptions.length; i++) {
        audioBoostOptionsMenu.items.push({
            text: audioBoostOptions[i] == activeAudioSetting ? `${audioBoostOptions[i]} <b>(ACTIVE)</b>` : audioBoostOptions[i],
            action: () => {
                gainNode.gain.value = i + 1
                activeAudioSetting = audioBoostOptions[i]
                InitMenus();
                closeMenu();
                openMenu(audioBoostOptionsMenu);
            }
        })
    }
}

function InitEvents() {
    $("#stop-button").on("click", () => ReturnTo())
    $(clickable_area).on("click", () => TogglePlay())
    $(play_button).on("click", () => TogglePlay())
    $("#progress-clickable").on('click', e => {
        Seek(((e.pageX - (e.target.offsetLeft + e.target.offsetParent.offsetLeft)) / e.target.offsetWidth) * videoDuration);
    });
    $("#progress-clickable").on('mousedown', () => {
        draggingVideoProgress = true;
    });
    $("#progress-clickable").on('mouseover', () => {
        hoveringOnVideoProgress = true
    });

    $("#progress-clickable").on('mouseout', () => {
        $("#hover-popup")[0].style.display = "";
        $("#hover-popup")[0].style.left = "";
        $("#potential-progress-bar")[0].style.width = `0px`;
        hoveringOnVideoProgress = false
    });

    $("#hover-popup img")[0].onerror = null

    $("#volume-bar").on('mousedown', () => {
        draggingAudioProgress = true;
        if (player.muted())
            player.muted(false);
    });

    $(document).on('mouseup', e => {
        if (draggingVideoProgress) {
            let pos = (e.pageX - ($("#progress-clickable")[0].offsetLeft + $("#progress-clickable")[0].offsetParent.offsetLeft)) / $("#progress-clickable")[0].offsetWidth;
            playTimeOffset = pos * videoDuration;
            draggingVideoProgress = false;
            progress_bar.style.transition = "";
        } else if (draggingAudioProgress) {
            let pos = (e.pageX - ($("#volume-bar")[0].offsetLeft + $("#volume-bar")[0].offsetParent.offsetLeft)) / $("#volume-bar")[0].offsetWidth;
            $("#volume-head")[0].style.width = `${pos * 100}px`;
            player.volume(pos);
            draggingAudioProgress = false;
        }
    });
    $(document).on('mousemove', e => {
        if (draggingVideoProgress) {
            let pos = (e.pageX - ($("#progress-clickable")[0].offsetLeft + $("#progress-clickable")[0].offsetParent.offsetLeft)) / $("#progress-clickable")[0].offsetWidth;
            progress_bar.style.transition = "0s";
            let percent = pos * 100;
            progress_bar.style.maxWidth = `${percent}%`;
            progress_bar.style.width = `${percent}%`;
        } else if (draggingAudioProgress) {
            let pos = (e.pageX - ($("#volume-bar")[0].offsetLeft + $("#volume-bar")[0].offsetParent.offsetLeft)) / $("#volume-bar")[0].offsetWidth;
            $("#volume-head")[0].style.width = `${pos * 100}px`;
            player.volume(pos);
        } else if (hoveringOnVideoProgress) {
            let pos = (e.pageX - ($("#progress-clickable")[0].offsetLeft + $("#progress-clickable")[0].offsetParent.offsetLeft)) / $("#progress-clickable")[0].offsetWidth;
            $("#hover-popup")[0].style.left = `calc(${pos * 100}% - ${$("#hover-popup")[0].offsetWidth / 2}px)`;
            hoveringOnVideoProgress = true
            $("#hover-popup")[0].style.display = `flex`;
            let seconds = pos * videoDuration;
            $("#hover-popup p")[0].innerText = GetStringTimeFromSeconds(seconds);
            $("#hover-popup img")[0].src = `${window.location.origin}/api/get/images?library=${library}&id=${id}&type=stills&duration=${Math.floor(seconds)}`;
            $("#potential-progress-bar")[0].style.width = `${pos * 100}%`;
        }
    });
    $(clickable_area).on("mousemove", () => {
        clearTimeout(interfaceTimer);
        ShowInterface();
        if (!video.paused)
            interfaceTimer = setTimeout(() => HideInterface(), 2000);
    })
    $("#options-button").on("click", () => {
        openMenu(optionsMenu);
    })
    $(clickable_area).dblclick(() => ToggleFullscreen())
    $(video).on("timeupdate", () => Update())
    $(document).on("keydown", e => {
        switch (e.key) {
            case "f":
            case "m":
                ToggleMute();
                break;
            case " ":
                TogglePlay();
                break;
            case "ArrowLeft":
                Skip(false)
                break;
            case "ArrowRight":
                Skip()
                break;
            case "ArrowUp":
                if (player.volume() != 1) {
                    if (player.muted()) player.muted(false);
                    if (player.volume() + .1 > 1)
                        player.volume(1);
                    else
                        player.volume(player.volume() + .1);
                }
                break;
            case "ArrowDown":
                if (player.volume() != 0) {
                    if (player.volume() - .1 <= .1) {
                        player.volume(0);
                        player.muted(true);
                    }
                    else
                        player.volume(player.volume() - .1);
                }
                break;
            default:
                break;
        }
    })
    $(video).on('volumechange', () => {
        console.log(player.volume())
        $("#volume-head")[0].style.width = player.volume() * 100 + "px";
        let icon = $("#volume-control i")[0]
        icon.classList = "";
        if (player.volume() > .5) {
            icon.classList.add("fa-solid", "fa-volume-high")
        } else if (player.volume() <= .5) {
            icon.classList.add("fa-solid", "fa-volume-low")
        } else if (player.volume() < .25) {
            icon.classList.add("fa-solid", "fa-volume-off")
        } else if (player.volume() < .1) {
            player.muted(true);
        }
        if (player.muted()) {
            icon.classList = "";
            icon.classList.add("fa-solid", "fa-volume-xmark")
        }
    })
    $("#volume-control i").on('click', () => {
        ToggleMute();
    })
    $("#progress-track, #controls, #top-row").on("mousemove", () => {
        clearTimeout(interfaceTimer);
        ShowInterface();
    })
    $(document).contextmenu(OpenSettings)
    $(player).on("progress", () => {
        try {
            let buffered_percentage = player.bufferedPercent(player.buffered(), videoDuration);
            $("#progress-buffer-bar")[0].style.maxWidth = `${buffered_percentage}%`;
            $("#progress-buffer-bar")[0].style.width = `${buffered_percentage}%`;
        } catch { }
    })
    if (!isTrailer) {
        setInterval(() => PingServer(), 5 * 1000);
    }
    $(player).on("waiting", () => {
        console.log("ARE WE BUFFERING")
    })
}

function Update() {
    durationWithOffset = playTimeOffset + video.currentTime;

    let date = new Date(new Date().getTime() + (videoDuration - durationWithOffset) * 1000);
    let hours = date.getHours();
    let minutes = date.getMinutes();
    let twelveHour = hours >= 12 ? 'P.M.' : 'A.M.';
    hours = hours % 12;
    hours = hours ? hours : 12;
    minutes = minutes < 10 ? '0' + minutes : minutes;
    let endTime = `${hours}:${minutes} ${twelveHour}`;
    $("#currentTime")[0].innerHTML = GetStringTimeFromSeconds(durationWithOffset);
    $("#totalTime")[0].innerHTML = GetStringTimeFromSeconds(videoDuration);
    $("#endTime")[0].innerHTML = endTime;

    let percent = (durationWithOffset / videoDuration) * 100;
    progress_bar.style.maxWidth = `${percent}%`;
    progress_bar.style.width = `${percent}%`;
    if (durationWithOffset >= videoDuration) {
        console.log(`duration: ${durationWithOffset}, video: ${videoDuration}`)
        ReturnTo();
    }
}

function OpenSettings() {
    openMenu(optionsMenu);
}

function TogglePlay() {
    PingServer();
    if (video.paused) {
        Play();
    } else {
        Pause();
    }
    Update();
}

function Play() {
    video.play();
    large_play_button.classList.remove('active')
    play_button.innerHTML = `<i class="fa-solid fa-pause"></i>`;
    HideInterface();
    clearTimeout(interfaceTimer);
    throbber.remove();
}

function Pause() {
    video.pause();
    large_play_button.classList.add('active')
    play_button.innerHTML = `<i class="fa-solid fa-play"></i>`;
    ShowInterface();
}
function Seek(position) {
    position = Math.floor(position);
    if (!isTrailer) {
        SetVideoSource(position)
    } else {
        video.currentTime = position;
    }
    playTimeOffset = position;
}
function Skip(forward = true) {
    PingServer();
    if (forward) {
        Seek(playTimeOffset + skipAmount);
    } else {
        Seek(playTimeOffset - skipAmount);
    }
}
function ToggleFullscreen() {
    if (isFullscreen) {
        document.exitFullscreen();
    } else {
        $("body")[0].requestFullscreen()
    }
    isFullscreen = !isFullscreen
}
function ShowInterface() {
    interface.classList.add('active');
}
function HideInterface() {
    interface.classList.remove('active');
}

function ToggleMute() {
    player.muted(!player.muted())
}

function GetStringTimeFromSeconds(seconds) {
    let totalHours = Math.floor(seconds / 60 / 60);
    let totalMinutes = Math.floor(seconds / 60) - (totalHours * 60);
    let totalSeconds = Math.floor(seconds % 60);

    totalHours = (totalHours > 9 ? totalHours : "0" + totalHours);
    totalMinutes = totalMinutes > 9 ? totalMinutes : "0" + totalMinutes;
    totalSeconds = totalSeconds > 9 ? totalSeconds : "0" + totalSeconds;

    return `${totalHours}:${totalMinutes}:${totalSeconds}`;
}

async function ChangeQualitySetting(quality, startPos = 0) {
    let pos = startPos == 0 ? Math.floor(durationWithOffset) : startPos;
    activeQualitySetting = quality;
    await SetVideoSource(pos)
    InitMenus()
}
async function SetVideoSource(startPos = 0) {
    if (!isTrailer && !debugMode) {
        $("main")[0].appendChild(throbber);
        player.dispose()
        Pause();
        playTimeOffset = startPos;
        let url = `${window.location.origin}/api/stream/get/version?id=${id}&username=${user}&version=${activeQualitySetting}&library=${library}&start_time=${startPos}&platform=${GetBrowser()}`;
        if (library == "tv")
            url += `&season=${season}&episode=${episode}`;
        if (streamStartTick != 0)
            await EndCurrentStream();
        if (streamStartTick == 0) {
            let s = `${window.location.origin}/api/stream/start?id=${id}&username=${user}&version=${activeQualitySetting}&library=${library}&start_time=${startPos}&platform=${GetBrowser()}`;
            if (library == "tv")
                s += `&season=${season}&episode=${episode}`;
            console.log(s)
            let response = await fetch(s)
            let data = await response.json();
            streamStartTick = data.uuid;
        }
        url += `&startTick=${streamStartTick}`;

        $("main")[0].appendChild(video);
        videojs(video, {
            controls: true,
            autoplay: true,
            preload: 'auto',
        }).ready(function () {
            player = this;
            player.src({ type: streamInfo.mime, src: url })
        })
        Play();
    }
}
async function PingServer() {
    if (!isTrailer) {
        let url = `${window.location.origin}/api/stream/get/stream_info?id=${id}&username=${user}&library=${library}&version=${activeQualitySetting}&startTime=${streamStartTick}&platform=${GetBrowser()}`;
        if (library == "tv")
            url += `&season=${season}&episode=${episode}`;
        streamInfo = await (await fetch(url)).json();

        let buffered_percentage = streamInfo.currentPosition / videoDuration * 100;
        $("#progress-buffer-bar")[0].style.maxWidth = `${buffered_percentage}%`;
        $("#progress-buffer-bar")[0].style.width = `${buffered_percentage}%`;

        videoDuration = streamInfo.maxPosition;

        durationWithOffset = playTimeOffset + video.currentTime;
        if (durationWithOffset > 30)
            $.post(`${window.location.origin}/api/post/watched_duration`, { id: id, username: user, duration: Math.floor(durationWithOffset), library: library, season: season == null ? 0 : season, episode: episode == null ? 0 : episode });
        if (durationWithOffset / videoDuration > .9)
            $.post(`${window.location.origin}/api/post/watched`, { id: id, username: user, watched: true, library: library, season: season == null ? 0 : season, episode: episode == null ? 0 : episode });

        // [FromForm] string id, [FromForm] string username, [FromForm] string version, [FromForm] long startTime, [FromForm] Platform platform, [FromForm] string library, [FromForm] int? season, [FromForm] int? episode, [FromForm] PlayState state, [FromForm] int currentPosition
        $.post(`${window.location.origin}/api/stream/update`, { id: id, username: user, currentPosition: Math.floor(durationWithOffset), state: video.paused && !bufferingDetected ? "Paused" : bufferingDetected ? "Buffering" : "Playing", library: library, season: season == null ? 0 : season, episode: episode == null ? 0 : episode, platform: GetBrowser(), version: activeQualitySetting, startTime: streamStartTick });
    }
}
function CheckBuffering() {
    try {
        currentPlayPos = playTimeOffset

        let offset = (updateBufferInterval - 20) / 1000

        if (!bufferingDetected && currentPlayPos < (lastPlayPos + offset) && !video.paused) {
            $("main")[0].appendChild(throbber)
            bufferingDetected = true
        }

        if (bufferingDetected && currentPlayPos > (lastPlayPos + offset) && !video.paused) {
            bufferingDetected = false
            throbber.remove();
        }
        lastPlayPos = currentPlayPos
    } catch (e) {
        console.error("Hi, we couldn't check your buffer progress")
        console.error(e)
    }
}

async function EndCurrentStream() {
    if (!isTrailer) {
        if (streamStartTick == 0) {
            return;
        }
        Pause()
        let data = new FormData();
        data.append("id", id)
        data.append("username", user)
        data.append("library", library)
        data.append("version", activeQualitySetting)
        data.append("startTime", streamStartTick)
        data.append("platform", GetBrowser())
        if (library == "tv") {
            data.append("season", season)
            data.append("episode", episode)
        }
        await fetch(`${window.location.origin}/api/stream/remove`, { method: "POST", body: data })
        streamStartTick = 0;
    }
}

function ReturnTo() {
    PingServer();
    EndCurrentStream();
    window.history.back();
}

// }).call();