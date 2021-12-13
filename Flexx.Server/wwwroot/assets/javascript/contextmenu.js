$("*").contextmenu(e => e.preventDefault())
function openMenu(object) {
    let menu = $("#context-menu")[0]
    clearContextMenu(menu);
    let title = document.createElement("div");
    title.id = "context-title";
    object.title = object.title == null ? "Options" : object.title;
    title.innerHTML = object.title;
    let menuItems = document.createElement("div");
    menuItems.id = "context-menu-items"
    menu.appendChild(title)
    menu.appendChild(menuItems)
    Array.from(object.items).forEach(item => {
        if (Object.keys(item).length === 0 || !item.disabled)
            addContextItem(menuItems, item.text, item.action);
    })
    menu.classList.add("active")
    $("body")[0].style.overflow = "hidden"
    addContextItem(menuItems, "Back", () => closeMenu())
    let maxLength = 0;
    let lines = 0;
    Array.from(object.title.split(' ')).forEach(m => {
        lines++;
        maxLength = m.length > maxLength ? m.length : maxLength;
    })
    maxLength = maxLength == 0 ? object.title.length : maxLength;
    let size = window.screen.height / maxLength;

    title.style.fontSize = `${size}px`;
}
function closeMenu() {
    let menu = $("#context-menu")[0]
    menu.classList.remove("active")
    $("body")[0].style.overflow = ""
}
function clearContextMenu(menu) {
    menu.innerHTML = "";
}
function addContextItem(menu, text, action) {
    let item = document.createElement("div");
    item.innerHTML = text;
    item.classList.add('context-item');
    menu.appendChild(item)
    $(item).on('click', () => action());
}