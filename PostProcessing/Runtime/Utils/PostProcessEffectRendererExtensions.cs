using System;

namespace UnityEngine.Rendering.PostProcessing
{
    static class PostProcessEffectRendererExtensions
    {
        /// <summary>
        /// Render with a try catch for all exception.
        ///
        /// If an exception occurs during the <see cref="PostProcessEffectRenderer.Render"/> call, it will be logged
        /// and returned.
        ///
        /// Use this method instead of <see cref="PostProcessEffectRenderer.Render"/> in critical contexts
        /// to avoid entering the exception flow.
        /// </summary>
        /// <param name="self">The renderer to render.</param>
        /// <param name="context">A context object</param>
        /// <returns></returns>
        public static Exception RenderOrLog(this PostProcessEffectRenderer self, PostProcessRenderContext context)
        {
            try
            {
                self.Render(context);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return e;
            }

            return null;
        }
    }
}
