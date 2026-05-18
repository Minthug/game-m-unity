mergeInto(LibraryManager.library, {
  // Unity → Web: haptic
  TriggerHaptic: function(typePtr) {
    var type = UTF8ToString(typePtr);
    if (window.onUnityMessage) {
      window.onUnityMessage(JSON.stringify({ type: 'haptic', payload: type }));
    }
  },

  // Unity → Web: storage write
  StorageSet: function(keyPtr, valuePtr) {
    var key   = UTF8ToString(keyPtr);
    var value = UTF8ToString(valuePtr);
    if (window.onUnityMessage) {
      window.onUnityMessage(JSON.stringify({ type: 'storage_set', key: key, value: value }));
    }
  },

  // Unity → Web: storage read (callback 방식)
  StorageGet: function(keyPtr) {
    var key = UTF8ToString(keyPtr);
    if (window.onUnityMessage) {
      window.onUnityMessage(JSON.stringify({ type: 'storage_get', key: key }));
    }
  },

  // Unity → Web: 텍스트 입력창 요청 (온보딩 마지막 단계)
  RequestTextInputJS: function(placeholderPtr) {
    var placeholder = UTF8ToString(placeholderPtr);
    if (window.onUnityMessage) {
      window.onUnityMessage(JSON.stringify({ type: 'request_text_input', placeholder: placeholder }));
    }
  },
});
