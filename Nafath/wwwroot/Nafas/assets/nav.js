
const hideLoadingElements = () => {
    document.querySelectorAll(".loading, .loader").forEach(element => {
        element.style.display = "none";
    });
};

if (localStorage.getItem("done")) {
    hideLoadingElements();
} else {
    window.addEventListener("load", () => {
        window.setTimeout(() => {
            hideLoadingElements();
            localStorage.setItem("done", "1");
        }, 1500);
    }, { once: true });
}
