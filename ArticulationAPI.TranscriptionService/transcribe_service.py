#!/usr/bin/env python3
import sys
import json
import re
import warnings

# Игнорируем предупреждения NLTK
warnings.filterwarnings('ignore')

# Импортируем g2p-en (это библиотека с 130,000+ слов!)
try:
    from g2p_en import G2p
    g2p = G2p()
    USE_G2P = True
    print("✅ Using g2p-en library", file=sys.stderr)
except Exception as e:
    print(f"⚠️ g2p-en not available, using fallback: {e}", file=sys.stderr)
    USE_G2P = False

def arpabet_to_ipa(symbol):
    """Конвертирует ARPABET в IPA символы"""
    mapping = {
        "AA": "ɑ", "AE": "æ", "AH": "ʌ", "AO": "ɔ",
        "AW": "aʊ", "AY": "aɪ", "B": "b", "CH": "tʃ",
        "D": "d", "DH": "ð", "EH": "ɛ", "ER": "ɝ",
        "EY": "eɪ", "F": "f", "G": "ɡ", "HH": "h",
        "IH": "ɪ", "IY": "i", "JH": "dʒ", "K": "k",
        "L": "l", "M": "m", "N": "n", "NG": "ŋ",
        "OW": "oʊ", "OY": "ɔɪ", "P": "p", "R": "ɹ",
        "S": "s", "SH": "ʃ", "T": "t", "TH": "θ",
        "UH": "ʊ", "UW": "u", "V": "v", "W": "w",
        "Y": "j", "Z": "z", "ZH": "ʒ"
    }
    # Убираем цифры ударения
    clean = ''.join([c for c in symbol if not c.isdigit()])
    return mapping.get(clean, clean.lower())

def transcribe_word_with_g2p(word):
    """Использует g2p-en для транскрипции слова"""
    try:
        # g2p возвращает список ARPABET символов: ['S', 'T', 'R', 'IH1', 'NG']
        arpabet_list = g2p(word)
        
        phonemes = []
        for arpabet in arpabet_list:
            ipa = arpabet_to_ipa(arpabet)
            phonemes.append(ipa)
        
        return phonemes
    except Exception as e:
        print(f"⚠️ Error transcribing '{word}': {e}", file=sys.stderr)
        # Fallback: побуквенно
        return list(word.lower())

def transcribe_word_fallback(word):
    """Fallback: побуквенная транскрипция"""
    return list(word.lower())

def transcribe_word(word):
    """Возвращает список IPA фонем для одного слова"""
    if USE_G2P:
        return transcribe_word_with_g2p(word)
    else:
        return transcribe_word_fallback(word)

def process_text(text):
    """
    Разбивает текст на слова и знаки препинания.
    """
    tokens = re.findall(r'[\w\']+|[.,!?;: ]', text)
    
    result = []
    for token in tokens:
        if token == ' ':
            result.append({"type": "space", "text": token, "phonemes": []})
        elif re.match(r'[\w\']+', token):
            phonemes = transcribe_word(token)
            result.append({"type": "word", "text": token, "phonemes": phonemes})
        elif token in ['.', ',', '!', '?', ';', ':']:
            result.append({"type": "punctuation", "text": token, "phonemes": []})
    
    return result

if __name__ == "__main__":
    if len(sys.argv) > 1:
        text = ' '.join(sys.argv[1:])
        result = process_text(text)
        print(json.dumps(result, ensure_ascii=False))