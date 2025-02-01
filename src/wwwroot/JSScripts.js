
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

function addClassToElement(identifier, className) {
    var element = document.getElementById(identifier) || document.querySelector(identifier);
    if (element) {
        element.classList.add(className);
        console.log('added');
    } else {
        console.warn('Element with ID or class "' + identifier + '" not found.');
    }
}

function removeClassFromElement(identifier, className) {
    var element = document.getElementById(identifier) || document.querySelector(identifier);
    if (element) {
        element.classList.remove(className);
    } else {
        console.warn('Element with ID or class "' + identifier + '" not found.');
    }
}

window.addGlobalKeyListener = (dotNetHelper) => {
    document.addEventListener("keydown", (event) => {
        if (event.code === "Space") { 
            dotNetHelper.invokeMethodAsync("SpaceKeyPressed");
        }
    });
};

function createStarEffectWithSVG(elementSelector, svgPath, options) {
    const element = document.querySelector(elementSelector);
    if (!element) {
        console.error(`Element not found: ${elementSelector}`);
        return;
    }

    // Получаем центр блока
    const rect = element.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    // Создаем контейнер для анимации
    const container = document.createElement("div");
    container.style.position = "absolute";
    container.style.left = "0";
    container.style.top = "0";
    container.style.width = "100%";
    container.style.height = "100%";
    container.style.pointerEvents = "none";
    document.body.appendChild(container);

    // Устанавливаем параметры из options с дефолтными значениями
    const {
        count = 15, // Количество звездочек
        range = 100, // Дальность разлета
        lifeTime = 2000, // Время жизни звездочек (в мс)
        gravity = 100 // Сила гравитации (чем выше значение, тем быстрее падают)
    } = options;

    // Получаем размеры окна
    const windowWidth = window.innerWidth;
    const windowHeight = window.innerHeight;

    // Функция для создания одной частицы
    const createSVGParticle = () => {
        // Оборачиваем SVG path в <svg> и используем RGB цвет
        const svgIcon = `
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="rgb(255, 255, 255)" width="24px" height="24px">
                ${svgPath}
            </svg>
        `;

        const wrapper = document.createElement("div");
        wrapper.innerHTML = svgIcon.trim(); // Вставка SVG-строки
        const svgElement = wrapper.firstElementChild;

        if (svgElement) {
            // Устанавливаем начальное абсолютное положение для частицы
            svgElement.style.position = "absolute";
            svgElement.style.left = `${centerX}px`;
            svgElement.style.top = `${centerY}px`;
            svgElement.style.transform = `translate(0, 0)`; // Начинаем из центра
            svgElement.style.opacity = "1";
            svgElement.style.transition = `transform ${lifeTime / 1000}s ease, opacity ${lifeTime / 1000}s ease`;

            // Генерируем случайные начальные направления
            const angle = Math.random() * 2 * Math.PI; // Угол в радианах
            const distance = Math.random() * range; // Случайное расстояние от центра

            let offsetX = Math.cos(angle) * distance;
            let offsetY = Math.sin(angle) * distance;

            // Рассчитываем конечные координаты частицы
            const finalX = centerX + offsetX;
            const finalY = centerY + offsetY;

            // Ограничиваем движение частиц по горизонтали, чтобы они не выходили за пределы окна
            if (finalX < 0) {
                offsetX = -centerX; // Останавливаем частицу у левой границы окна
            } else if (finalX + 24 > windowWidth) {
                offsetX = windowWidth - centerX - 24; // Останавливаем частицу у правой границы окна
            }

            // Ограничиваем движение частиц по вертикали, чтобы они не выходили за пределы окна
            if (finalY < 0) {
                offsetY = -centerY; // Останавливаем частицу у верхней границы окна
            } else if (finalY + 24 > windowHeight) {
                offsetY = windowHeight - centerY - 24; // Останавливаем частицу у нижней границы окна
            }

            // Добавляем элемент в контейнер
            container.appendChild(svgElement);

            // Убеждаемся, что начальное положение применяется
            requestAnimationFrame(() => {
                // Сначала разлетаются в стороны
                svgElement.style.transform = `translate(${offsetX}px, ${offsetY}px)`;

                // Затем подвергаются гравитации (движение вниз после разлета)
                setTimeout(() => {
                    // Рассчитываем конечное смещение с учетом гравитации
                    const gravityY = offsetY + gravity;
                    const finalGravityY = centerY + gravityY;

                    // Ограничиваем движение вниз с учетом гравитации, чтобы частицы не выходили за нижнюю границу окна
                    if (finalGravityY + 24 > windowHeight) {
                        svgElement.style.transform = `translate(${offsetX}px, ${windowHeight - centerY - 24}px)`;
                    } else {
                        svgElement.style.transform = `translate(${offsetX}px, ${gravityY}px)`;
                    }

                    svgElement.style.opacity = "0"; // Исчезают через время жизни
                }, lifeTime * 0.6); // Гравитация включается после половины времени жизни
            });

            // Удаляем элемент после завершения времени жизни
            setTimeout(() => {
                svgElement.remove();
            }, lifeTime);
        } else {
            console.error("Invalid SVG path provided!");
        }
    };

    // Генерируем частицы в указанном количестве
    for (let i = 0; i < count; i++) {
        setTimeout(createSVGParticle, i * 10); // Появляются с небольшой задержкой
    }

    // Удаляем контейнер после завершения эффекта
    setTimeout(() => container.remove(), lifeTime + 1000);
}
