// Хранение всех аудио элементов с их ID
let nextAudioId = 1;
const audioElements = {};
let currentAudioId = null;

// Функция для воспроизведения аудио из base64 строки
function playAudio(base64String, mimeType, playbackSpeed = 1.0, volume = 1.0) {
    console.log(`Playing audio with MIME type: ${mimeType}, requested speed: ${playbackSpeed}, volume: ${volume}`);

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