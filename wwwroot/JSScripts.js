
var userScrolled = false;

function scrollToBottom(elementId) {
    if (!userScrolled) {

        var element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
        else {
            console.error("Element not found " + elementId);
        }
    }

}
function initializeScrollTracker(elementId) {
    var element = document.getElementById(elementId);
    if (element) {

        element.removeEventListener("wheel", unblockUserScroll);
        element.removeEventListener("touchstart", unblockUserScroll);
        element.removeEventListener("touchmove", unblockUserScroll);
        console.log("chat tracker go " + elementId);
        element.addEventListener("wheel", unblockUserScroll);
        element.addEventListener("touchstart", unblockUserScroll);
        element.addEventListener("touchmove", unblockUserScroll);
    }
}

function unblockUserScroll(event) {

        console.log("UnblockScroll");
        userScrolled = true;
  
}
function resetUserScroll() {
    userScrolled = false;
}

window.downloadFile = (dataUrl, fileName) => {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = fileName;
    link.click();
};


