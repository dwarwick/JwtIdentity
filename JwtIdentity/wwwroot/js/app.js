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

    for (var i = elements.length - 1, l = elements.length; i >= 0; i--) {
        elements[i].remove();
    }
}