#!/usr/bin/env python3
import sys
import json
import re
import warnings

warnings.filterwarnings('ignore')

# Большой словарь с правильной IPA транскрипцией
RUSSIAN_IPA_DICT = {
    # Базовые слова
    "корова": ["k", "ɐ", "r", "o", "v", "ə"],
    "корыто": ["k", "ɐ", "r", "ɨ", "t", "ə"], 
    "аллегория": ["ɐ", "lʲ", "ɪ", "g", "o", "rʲ", "ɪ", "j", "ə"],
    "молоко": ["m", "ə", "l", "ɐ", "k", "o"],
    "привет": ["p", "rʲ", "ɪ", "v", "j", "e", "t"],
    "мир": ["m", "i", "r"],
    "мама": ["m", "a", "m", "a"],
    "папа": ["p", "a", "p", "a"],
    "город": ["g", "o", "r", "ə", "t"],
    "собака": ["s", "ɐ", "b", "a", "k", "ə"],
    "кошка": ["k", "o", "ʂ", "k", "ə"],
    "мышь": ["m", "ɨ", "ʂ"],
    "человек": ["tɕ", "ɪ", "l", "ɐ", "v", "j", "e", "k"],
    "нога": ["n", "ɐ", "g", "a"],
    "голова": ["g", "ɐ", "l", "ɐ", "v", "a"],
    "сердце": ["s", "ʲ", "e", "r", "t", "s", "e"],
}

# Простое сопоставление букв для fallback
RUSSIAN_SIMPLE = {
    'а': 'a', 'б': 'b', 'в': 'v', 'г': 'g', 'д': 'd',
    'е': 'je', 'ё': 'jo', 'ж': 'ʐ', 'з': 'z', 'и': 'i',
    'й': 'j', 'к': 'k', 'л': 'l', 'м': 'm', 'н': 'n',
    'о': 'o', 'п': 'p', 'р': 'r', 'с': 's', 'т': 't',
    'у': 'u', 'ф': 'f', 'х': 'x', 'ц': 'ts', 'ч': 'tɕ',
    'ш': 'ʂ', 'щ': 'ɕː', 'ы': 'ɨ', 'э': 'e', 'ю': 'ju', 'я': 'ja'
}

def apply_vowel_reduction(phonemes, word):
    """
    Применяет базовые правила редукции гласных в русском языке
    """
    if len(phonemes) <= 1:
        return phonemes
    
    result = []
    word_lower = word.lower()
    
    # Упрощённо считаем, что ударение на предпоследнем слоге
    # В реальности нужно определять ударение, но для демо это ок
    for i, phoneme in enumerate(phonemes):
        # Применяем редукцию к 'о' и 'а'
        if phoneme in ['o', 'a']:
            # В конце слова (кроме односложных)
            if i == len(phonemes) - 1 and len(phonemes) > 2:
                result.append('ə')  # редукция до 'ə'
            # В безударных позициях (упрощённо)
            elif i < len(phonemes) - 2 and len(phonemes) > 3:
                result.append('ɐ')  # редукция до 'ɐ'
            else:
                result.append(phoneme)
        # Редукция 'я' после гласных
        elif phoneme == 'ja' and i > 0 and phonemes[i-1] in ['a', 'o', 'u', 'i']:
            result.append('jə')
        else:
            result.append(phoneme)
    
    return result

def transcribe_with_rules(word):
    """
    Транскрипция с правилами редукции
    """
    word_lower = word.lower()
    
    # 1. Сначала проверяем словарь
    if word_lower in RUSSIAN_IPA_DICT:
        return RUSSIAN_IPA_DICT[word_lower]
    
    # 2. Если слова нет — применяем правила
    phonemes = []
    for char in word_lower:
        if char in RUSSIAN_SIMPLE:
            phonemes.append(RUSSIAN_SIMPLE[char])
        else:
            phonemes.append(char)
    
    # 3. Применяем редукцию гласных
    phonemes = apply_vowel_reduction(phonemes, word_lower)
    
    return phonemes

def process_russian_text(text):
    """Разбивает текст на токены"""
    tokens = re.findall(r'[а-яА-ЯёЁ\']+|[.,!?;: ]', text)
    
    result = []
    for token in tokens:
        if token == ' ':
            result.append({"type": "space", "text": token, "phonemes": []})
        elif re.match(r'[а-яА-ЯёЁ\']+', token):
            phonemes = transcribe_with_rules(token)
            result.append({"type": "word", "text": token, "phonemes": phonemes})
        elif token in ['.', ',', '!', '?', ';', ':']:
            result.append({"type": "punctuation", "text": token, "phonemes": []})
    
    return result

if __name__ == "__main__":
    if len(sys.argv) > 1:
        text = ' '.join(sys.argv[1:])
        result = process_russian_text(text)
        print(json.dumps(result, ensure_ascii=False))