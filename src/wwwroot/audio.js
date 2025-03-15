// Хранение всех аудио элементов с их ID
let nextAudioId = 1;
const audioElements = {};
let currentAudioId = null;

// Функция для воспроизведения аудио из base64 строки
function playAudio(base64String, mimeType, playbackSpeed = 1.0, volume = 1.0, useVisualizer = false) {
    console.log(`Playing audio with MIME type: ${mimeType}, requested speed: ${playbackSpeed}, volume: ${volume}, visualizer: ${useVisualizer}`);

    // Правильный MIME-тип для WAV
    if (mimeType === "audio/mp3" || mimeType === "audio/mpeg") {
        mimeType = "audio/mpeg"; // для MP3
    } else {
        mimeType = "audio/wav"; // для WAV по умолчанию
    }

    const audio = new Audio();
    const audioId = nextAudioId++;

    // Убедимся, что playbackSpeed - это число
    playbackSpeed = parseFloat(playbackSpeed);
    if (isNaN(playbackSpeed) || playbackSpeed <= 0) {
        console.warn("Некорректная скорость воспроизведения, установлено значение по умолчанию 1.0");
        playbackSpeed = 1.0;
    }

    // Убедимся, что volume - это число и в допустимых пределах (0.0-1.0)
    volume = parseFloat(volume);
    if (isNaN(volume) || volume < 0 || volume > 1) {
        console.warn("Некорректный уровень громкости, установлено значение по умолчанию 1.0");
        volume = 1.0;
    }

    // Отслеживание ошибок
    audio.addEventListener('error', (e) => {
        console.error("Audio error:", e);
        console.error("Audio element error code:", audio.error ? audio.error.code : "unknown");
        console.error("Audio element error message:", audio.error ? audio.error.message : "unknown");
    });

    audioElements[audioId] = audio;
    currentAudioId = audioId;

    // Установка источника данных
    audio.src = `data:${mimeType};base64,${base64String}`;

    // Установка скорости воспроизведения и громкости до загрузки
    audio.playbackRate = playbackSpeed;
    audio.volume = volume;

    // Загрузка аудио
    audio.load();

    // Добавляем обработчик события canplaythrough
    audio.addEventListener('canplaythrough', () => {
        // Повторно установим скорость и громкость после загрузки
        audio.playbackRate = playbackSpeed;
        audio.volume = volume;
    });

    // Настройка аудио визуализатора при необходимости
    if (useVisualizer) {
        setupAudioVisualizer(audio);
    }

    // Воспроизведение с обработкой ошибок
    audio.play().catch(err => {
        console.error("Ошибка воспроизведения", err);
        console.log("Base64 sample (начало):", base64String.substring(0, 50) + "...");
    });

    return audioId;
}

// Регистрация обратного вызова по завершении аудио
function registerAudioEndedCallback(audioId, dotNetCallback) {
    const audio = audioElements[audioId];
    if (!audio) {
        console.warn(`Audio element with ID ${audioId} not found for callback`);
        return;
    }

    audio.onended = () => {
        console.log(`Audio ${audioId} playback ended`);
        dotNetCallback.invokeMethodAsync('OnAudioEnded');
        delete audioElements[audioId];
        if (currentAudioId === audioId) {
            currentAudioId = null;
        }
    };
}

// Остановка конкретного аудио
function stopAudio(audioId) {
    const audio = audioElements[audioId];
    if (audio) {
        audio.pause();
        audio.currentTime = 0;

        // Остановка визуализации, если она была запущена
        if (window.visualizerAnimationFrame) {
            cancelAnimationFrame(window.visualizerAnimationFrame);
            window.visualizerAnimationFrame = null;
        }

        delete audioElements[audioId];
        if (currentAudioId === audioId) {
            currentAudioId = null;
        }
    } else {
        console.warn(`Attempted to stop non-existent audio ID: ${audioId}`);
    }
}

// Остановка текущего аудио
function stopCurrentAudio() {
    if (currentAudioId) {
        stopAudio(currentAudioId);
    } else {
        console.warn("No current audio to stop");
    }
}

// Пауза текущего аудио
function pauseCurrentAudio() {
    if (currentAudioId && audioElements[currentAudioId]) {
        audioElements[currentAudioId].pause();
    } else {
        console.warn("No current audio to pause");
    }
}

// Возобновление текущего аудио
function resumeCurrentAudio() {
    if (currentAudioId && audioElements[currentAudioId]) {
        audioElements[currentAudioId].play().catch(err => console.error("Ошибка возобновления", err));
    } else {
        console.warn("No current audio to resume");
    }
}

// Функция для остановки всех аудио
function stopAllAudio() {
    Object.values(audioElements).forEach(audio => {
        audio.pause();
        audio.currentTime = 0;
    });

    Object.keys(audioElements).forEach(id => {
        delete audioElements[id];
    });

    currentAudioId = null;
    console.log("All audio stopped");
}
function setupAudioVisualizer(audio) {
    console.log(`Setting up audio visualizer for track... ${audio.src.substring(0, 30)}...`);

    // Проверяем, нужно ли вообще настраивать визуализатор
    if (window.currentVisualizedAudio === audio) {
        console.log("This audio is already being visualized");
        return;
    }

    window.currentVisualizedAudio = audio;

    // Определяем размеры экрана пользователя
    const isMobile = window.innerWidth <= 768;
    const WIDTH = isMobile ? 300 : 1000;
    const HEIGHT = isMobile ? 50 : 200
    console.log(`Detected device: ${isMobile ? "Mobile" : "Desktop"}, using canvas size: ${WIDTH}x${HEIGHT}`);
    // Создаем или используем существующий холст
    let canvas = document.getElementById('audioVisualizer');
    if (!canvas) {
        canvas = document.createElement('canvas');
        canvas.id = 'audioVisualizer';
        canvas.width = WIDTH;
        canvas.height = HEIGHT;
        canvas.style.display = 'block';
        canvas.style.margin = '10px auto';
        document.body.appendChild(canvas);
    }

    const ctx = canvas.getContext("2d");

    // Функция для извлечения RGB-значений из CSS-переменных
    function getCssColorAsRGB(varName, fallbackColor) {
        // Получаем стили корневого элемента
        const styles = getComputedStyle(document.documentElement);

        // Получаем значение переменной с удалением пробелов
        let color = styles.getPropertyValue(varName).trim();

        // Если переменная не определена, используем fallback
        if (!color) {
            color = fallbackColor;
        }

        // Создаем временный элемент для получения RGB
        const temp = document.createElement('div');
        temp.style.color = color;
        temp.style.display = 'none';
        document.body.appendChild(temp);

        // Получаем вычисленный стиль, который всегда в формате rgb(r,g,b)
        const computedColor = getComputedStyle(temp).color;
        document.body.removeChild(temp);

        // Извлекаем RGB значения 
        const rgbMatch = computedColor.match(/rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)/);
        if (rgbMatch) {
            return [parseInt(rgbMatch[1]), parseInt(rgbMatch[2]), parseInt(rgbMatch[3])];
        }

        // Если не удалось распарсить, возвращаем значение по умолчанию
        return [0, 0, 0];
    }

    // Проверяем и создаем AudioContext
    if (!window.AudioContext && !window.webkitAudioContext) {
        console.error("AudioContext не поддерживается в этом браузере");
        return;
    }

    let audioContext;
    if (!window.visualizerAudioContext) {
        try {
            window.visualizerAudioContext = new (window.AudioContext || window.webkitAudioContext)();
        } catch (e) {
            console.error("Не удалось создать AudioContext:", e);
            return;
        }
    }
    audioContext = window.visualizerAudioContext;

    // Создаем или используем существующий анализатор
    if (!window.visualizerAnalyser) {
        window.visualizerAnalyser = audioContext.createAnalyser();
    }
    const analyser = window.visualizerAnalyser;

    // Подключаем аудио к анализатору
    try {
        if (!audio._connectedToVisualizer) {
            source = audioContext.createMediaElementSource(audio);
            source.connect(analyser);
            analyser.connect(audioContext.destination);
            audio._connectedToVisualizer = true;
        }
    } catch (e) {
        console.warn("Не удалось подключить аудио к анализатору:", e);
    }

    // Массив для хранения проанализированных частот
    const freqs = new Uint8Array(analyser.frequencyBinCount);

    // Параметры визуализации - используем глобальные настройки
    if (!window.visualizerOpts) {
        // Получаем цвета из CSS-переменных
        const color1 = getCssColorAsRGB('--mud-palette-primary', '#cb2480');
        const color2 = getCssColorAsRGB('--mud-palette-secondary', '#29c8c0');
        const color3 = getCssColorAsRGB('--mud-palette-tertiary', '#1889da');
        window.visualizerOpts = {
            smoothing: 0.9,
            fft: 8,
            minDecibels: -70,
            scale: 0.2,
            glow: 0,
            color1: color1,
            color2: color2,
            color3: color3,
            fillOpacity: 0.6,
            lineWidth: 2,
            blend: "screen",
            shift: 50,
            width: 60,
            amp: 0.8
        
        };
    }
    const opts = window.visualizerOpts;

    // Настраиваем GUI-контролы если доступен dat.GUI
    if (typeof dat !== 'undefined' && dat.GUI) {
        try {
            if (!window.visualizerGUI) {
                window.visualizerGUI = new dat.GUI();
                const gui = window.visualizerGUI;
                gui.close();

                gui.addColor(opts, "color1");
                gui.addColor(opts, "color2");
                gui.addColor(opts, "color3");
                gui.add(opts, "fillOpacity", 0, 1);
                gui.add(opts, "lineWidth", 0, 10).step(1);
                gui.add(opts, "glow", 0, 100);
                gui.add(opts, "blend", [
                    "normal", "multiply", "screen",
                    "overlay", "lighten", "difference"
                ]);
                gui.add(opts, "smoothing", 0, 1);
                gui.add(opts, "minDecibels", -100, 0);
                gui.add(opts, "amp", 0, 5);
                gui.add(opts, "width", 0, 60);
                gui.add(opts, "shift", 0, 200);
            }
        } catch (e) {
            console.warn("Ошибка настройки dat.GUI:", e);
        }
    }

    // Вспомогательная функция для создания числового диапазона
    function range(i) {
        return Array.from(Array(i).keys());
    }

    // Перемешиваем частоты
    const shuffle = [1, 3, 0, 4, 2];

    // Выбираем частоту для заданного канала и индекса значения
    function freq(channel, i) {
        const band = 2 * channel + shuffle[i] * 6;
        return band < freqs.length ? freqs[band] : 0;
    }

    // Возвращает масштабный коэффициент для заданного индекса
    function scale(i) {
        const x = Math.abs(2 - i);
        const s = 3 - x;
        return s / 3 * opts.amp;
    }

    // Рисуем путь визуализации
    function path(channel) {
        // Берем цвет из настроек
        const color = opts[`color${channel + 1}`].map(Math.floor);

        // Преобразуем массив [r,g,b] в rgba() CSS-цвет
        ctx.fillStyle = `rgba(${color}, ${opts.fillOpacity})`;

        // Устанавливаем обводку и тень того же цвета
        ctx.strokeStyle = ctx.shadowColor = `rgb(${color})`;

        ctx.lineWidth = opts.lineWidth;
        ctx.shadowBlur = opts.glow;
        ctx.globalCompositeOperation = opts.blend;

        const m = HEIGHT / 2;
        const offset = (WIDTH - 15 * opts.width) / 2;
        const x = range(15).map(i => offset + channel * opts.shift + i * opts.width);
        const y = range(5).map(i => Math.max(0, m - scale(i) * freq(channel, i)));
        const h = 2 * m;

        ctx.beginPath();
        ctx.moveTo(0, m);
        ctx.lineTo(x[0], m + 1);

        ctx.bezierCurveTo(x[1], m + 1, x[2], y[0], x[3], y[0]);
        ctx.bezierCurveTo(x[4], y[0], x[4], y[1], x[5], y[1]);
        ctx.bezierCurveTo(x[6], y[1], x[6], y[2], x[7], y[2]);
        ctx.bezierCurveTo(x[8], y[2], x[8], y[3], x[9], y[3]);
        ctx.bezierCurveTo(x[10], y[3], x[10], y[4], x[11], y[4]);

        ctx.bezierCurveTo(x[12], y[4], x[12], m, x[13], m);

        ctx.lineTo(WIDTH, m + 1);
        ctx.lineTo(x[13], m - 1);

        // Нижняя половина
        ctx.bezierCurveTo(x[12], m, x[12], h - y[4], x[11], h - y[4]);
        ctx.bezierCurveTo(x[10], h - y[4], x[10], h - y[3], x[9], h - y[3]);
        ctx.bezierCurveTo(x[8], h - y[3], x[8], h - y[2], x[7], h - y[2]);
        ctx.bezierCurveTo(x[6], h - y[2], x[6], h - y[1], x[5], h - y[1]);
        ctx.bezierCurveTo(x[4], h - y[1], x[4], h - y[0], x[3], h - y[0]);
        ctx.bezierCurveTo(x[2], h - y[0], x[1], m, x[0], m);

        ctx.lineTo(0, m);

        ctx.fill();
        ctx.stroke();
    }

    // Останавливаем существующую визуализацию
    if (window.visualizerAnimationFrame) {
        cancelAnimationFrame(window.visualizerAnimationFrame);
        window.visualizerAnimationFrame = null;
    }

    // Функция анимации для визуализации
    function visualize() {
        analyser.smoothingTimeConstant = opts.smoothing;
        analyser.fftSize = Math.pow(2, opts.fft);
        analyser.minDecibels = opts.minDecibels;
        analyser.maxDecibels = 0;
        analyser.getByteFrequencyData(freqs);

        canvas.width = WIDTH;
        canvas.height = HEIGHT;

        path(0);
        path(1);
        path(2);

        if (!audio.paused) {
            window.visualizerAnimationFrame = requestAnimationFrame(visualize);
        }
    }

    // Запускаем визуализацию при воспроизведении аудио
    function onPlay() {
        if (audioContext.state === 'suspended') {
            audioContext.resume().then(() => {
                window.visualizerAnimationFrame = requestAnimationFrame(visualize);
            });
        } else {
            window.visualizerAnimationFrame = requestAnimationFrame(visualize);
        }
    }

    // Обрабатываем события паузы и окончания
    function onPauseOrEnded() {
        if (window.visualizerAnimationFrame) {
            cancelAnimationFrame(window.visualizerAnimationFrame);
            window.visualizerAnimationFrame = null;
        }

        // Clear the frequency data by setting all values to 0
        for (let i = 0; i < freqs.length; i++) {
            freqs[i] = 0;
        }

        // Redraw with zeroed frequency data
        canvas.width = WIDTH;
        canvas.height = HEIGHT;
        path(0);
        path(1);
        path(2);

        if (window.currentVisualizedAudio === audio &&
            (audio.ended || audio.currentTime >= audio.duration)) {
            window.currentVisualizedAudio = null;
        }
    }

    // Обработчики событий
    audio.removeEventListener('play', onPlay);
    audio.removeEventListener('pause', onPauseOrEnded);
    audio.removeEventListener('ended', onPauseOrEnded);

    audio.addEventListener('play', onPlay);
    audio.addEventListener('pause', onPauseOrEnded);
    audio.addEventListener('ended', onPauseOrEnded);

    // Начинаем визуализацию если аудио уже воспроизводится
    if (!audio.paused) {
        onPlay();
    }
}