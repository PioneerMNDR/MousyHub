using System;
using System.IO;
using System.Text;

public class GGUFReader
{
    private const int ChunkSize = 8192; // Читаем только первые 8 КБ файла

    public static int[] ReadGGUFMetadata(string filePath)
    {
        try
        {
            // Проверяем размер файла
            long fileSize = new FileInfo(filePath).Length;
            if (fileSize < 10000) // Игнорируем файлы меньше 10 КБ
                return null;

            // Открываем файл для чтения
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Проверяем, что файл начинается с "GGUF"
                byte[] header = reader.ReadBytes(4);
                if (Encoding.ASCII.GetString(header) != "GGUF")
                    return null;

                // Читаем первые 8 КБ данных
                byte[] data = reader.ReadBytes(ChunkSize);

                // Извлекаем метаданные
                int layercount = ReadGGUFKey(data, ".block_count", 512);
                int headCountKv = ReadGGUFKey(data, ".attention.head_count_kv", 8192);
                int keyLength = ReadGGUFKey(data, ".attention.key_length", 8192);
                int valLength = ReadGGUFKey(data, ".attention.value_length", 8192);
     

                // Возвращаем результат
                return new int[] { layercount, headCountKv, Math.Max(keyLength, valLength) };
            }
        }
        catch
        {
            return null; // В случае ошибки возвращаем null
        }
    }

    private static int ReadGGUFKey(byte[] data, string keyName, int maxVal)
    {
        byte[] keyBytes = Encoding.ASCII.GetBytes(keyName);
        int keyLength = keyBytes.Length;

        // Ищем ключ в данных
        int index = FindSequence(data, keyBytes);
        if (index == -1 || index + keyLength + 8 > data.Length)
            return 0; // Ключ не найден

        // Читаем два 32-битных числа после ключа
        int startIndex = index + keyLength;
        uint value1 = BitConverter.ToUInt32(data, startIndex);
        uint value2 = BitConverter.ToUInt32(data, startIndex + 4);

        // Проверяем условия
        if (value1 == 4 && value2 > 0 && value2 <= maxVal)
            return (int)value2; // Возвращаем значение

        return 0; // Условия не выполнены
    }

    private static int FindSequence(byte[] data, byte[] sequence)
    {
        for (int i = 0; i <= data.Length - sequence.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (data[i + j] != sequence[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i; // Возвращаем индекс начала последовательности
        }
        return -1; // Последовательность не найдена
    }
}