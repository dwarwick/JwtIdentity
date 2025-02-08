export function addCss(fileName) {

    var head = document.head;
    var link = document.createElement("link");

    link.type = "text/css";
    link.rel = "stylesheet";
    link.href = fileName;
    link.className = "theme";

    head.appendChild(link);
}

export function removeThemes() {

    var elements = document.getElementsByClassName("theme");

    if (elements.length == 0) return;

    for (var i = 0, l = elements.length; i < l; i++) {
        elements[i].remove();
    }
}