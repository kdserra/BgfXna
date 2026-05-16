mergeInto(LibraryManager.library, {
  emscripten_webgl_enable_extension: function(contextHandle, extension) {
    var context = GL.getContext(contextHandle) || GL.currentContext;
    if (!context || !context.GLctx) {
      return 0;
    }

    GL.makeContextCurrent(context.handle);

    var extString = UTF8ToString(extension);
    if (extString.startsWith('GL_')) {
      extString = extString.substr(3);
    }

    var ext = context.GLctx.getExtension(extString);
    return !!ext;
  },

  emscripten_glGetInternalformativ: function(target, internalformat, pname, bufSize, params) {
    if (!params || bufSize <= 0) {
      return;
    }

    if (!GLctx || !GLctx.getInternalformatParameter) {
      for (var i = 0; i < bufSize; ++i) {
        HEAP32[(params >> 2) + i] = 0;
      }
      return;
    }

    var ret = GLctx.getInternalformatParameter(target, internalformat, pname);
    if (ret === null) {
      return;
    }

    for (var j = 0; j < ret.length && j < bufSize; ++j) {
      HEAP32[(params >> 2) + j] = ret[j];
    }
  }
});
