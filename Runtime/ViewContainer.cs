using Cysharp.Threading.Tasks;
using LazyCoder.Core;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LazyCoder.View
{
    public class ViewContainer : MonoSingleton<ViewContainer>
    {
        private List<View> _views = new List<View>();

        private bool _isTransiting = false;

        protected override bool PersistAcrossScenes { get { return false; } }

        #region Function -> Private

        private View GetTopView()
        {
            return _views.Count <= 0 ? null : _views.Last();
        }

        private void RemoveView(View view)
        {
            _views.Remove(view);
        }

        private void RevealTopView()
        {
            View topView = GetTopView();

            topView?.Reveal();
        }

        private void BlockTopView()
        {
            View topView = GetTopView();

            topView?.Block();
        }

        private void AddView(View view)
        {
            BlockTopView();

            // Handle view callback
            view.OnCloseStart.AddListener(() => { RemoveView(view); });
            view.OnCloseEnd.AddListener(() =>
            {
                Destroy(view.GameObjectCached);

                RevealTopView();
            });

            // Open new view
            view.Open();

            // Add new view into list
            _views.Add(view);
        }

        #endregion

        #region Function -> Public

        public async UniTask<View> PushAsync(AssetReference viewAsset, CancellationToken cancellationToken)
        {
            if (!CanPushNewView())
            {
                LDebug.Log<ViewContainer>($"Another View is transiting, can't push any new view {viewAsset}");
                return null;
            }

            // Set transiting flag
            _isTransiting = true;

            // Wait new view to be loaded
            var handle = Addressables.LoadAssetAsync<GameObject>(viewAsset);

            await handle.WithCancellation(cancellationToken);

            // Spawn view object from loaded asset
            View view = handle.Result.Create(TransformCached, false).GetComponent<View>();
            view.GameObjectCached.SetActive(false);

            // Release asset when view closed
            view.OnCloseEnd.AddListener(() => { handle.Release(); });

            AddView(view);

            _isTransiting = false;

            return view;
        }

        public View Push(GameObject viewPrefab)
        {
            if (!CanPushNewView())
            {
                LDebug.Log<ViewContainer>($"Another View is transiting, can't push any new view {viewPrefab}");
                return null;
            }

            // Spawn view object from prefab
            View view = viewPrefab.Create(TransformCached, false).GetComponent<View>();
            view.GameObjectCached.SetActive(false);

            AddView(view);

            return view;
        }

        public bool CanPushNewView()
        {
            return !_isTransiting;
        }

        #endregion
    }
}