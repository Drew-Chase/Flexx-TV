function GetBrowser() {
    // Opera 8.0+
    var isOpera = (!!window.opr && !!opr.addons) || !!window.opera || navigator.userAgent.indexOf(' OPR/') >= 0;

    // Firefox 1.0+
    var isFirefox = typeof InstallTrigger !== 'undefined';

    // Safari 3.0+ "[object HTMLElementConstructor]" 
    var isSafari = /constructor/i.test(window.HTMLElement) || (function (p) { return p.toString() === "[object SafariRemoteNotification]"; })(!window['safari'] || (typeof safari !== 'undefined' && safari.pushNotification));

    // Internet Explorer 6-11
    var isIE = /*@cc_on!@*/false || !!document.documentMode;

    // Edge 20+
    var isEdge = !isIE && !!window.StyleMedia;

    // Chrome 1 - 71
    var isChrome = !!window.chrome && (!!window.chrome.webstore || !!window.chrome.runtime);

    // Blink engine detection
    var isBlink = (isChrome || isOpera) && !!window.CSS;

    return isOpera ? "Opera" : isFirefox ? "Firefox" : isSafari ? "Safari" : isIE ? "InternetExplorer" : isEdge ? "Edge" : isChrome ? "Chrome" : isBlink ? "Blink" : "GenericWeb";
}


function GetBrowserIcon(browser = GetBrowser()) {
    switch (browser.toLowerCase()) {
        case "opera":
            return `<i class="fa-brands fa-opera"></i>`;
        case "chrome":
            return `<i class="fa-brands fa-chrome"></i>`;
        case "firefox":
            return `<i class="fa-brands fa-firefox-browser"></i>`;
        case "edge":
            return `<i class="fa-brands fa-edge"></i>`;
        case "safari":
            return `<i class="fa-brands fa-safari"></i>`;
        case "internetexplorer":
            return `<i class="fa-brands fa-internet-explorer"></i>`;
        case "blink":
            return `<i class="fa-brands fa-chrome"></i>`;
        default:
            return `<i class="fa-solid fa-globe"></i>`;
    }
}