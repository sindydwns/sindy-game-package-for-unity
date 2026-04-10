using System.Collections.Generic;
using R3;
using Sindy.Http;
using Sindy.View;
using Sindy.View.Components;
using Sindy.View.Features;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 5 — 페이지네이션 동작 검증
    ///
    /// 검증 항목:
    ///   Case1: 초기 로드 → 1페이지 요청
    ///   Case2: NextButton → 2페이지 요청
    ///   Case3: PrevButton → 이전 페이지 요청
    ///   Case4: 1페이지에서 PrevButton 비활성화
    ///   Case5: 마지막 페이지에서 NextButton 비활성화
    ///   Case6: Items PropModel이 ListComponent와 바인딩 가능한지
    /// </summary>
    class TestPaginationWork : TestCase
    {
        private class RankingDto
        {
            public string Name { get; set; }
            public int Rank { get; set; }
        }

        public override void Run()
        {
            Case1_InitialLoad();
            Case2_NextButton();
            Case3_PrevButton();
            Case4_PrevButtonDisabledOnPage1();
            Case5_NextButtonDisabledOnLastPage();
            Case6_ItemsPropModel();
        }

        private PaginatedApiModel<RankingDto> CreatePaginatedModel(IHttpClient client)
        {
            return new PaginatedApiModel<RankingDto>(client, "/api/ranking", dto =>
            {
                var vm = new ViewModel();
                vm["name"] = new PropModel<string>(dto.Name);
                vm["rank"] = new PropModel<int>(dto.Rank);
                return vm;
            });
        }

        // ── Case 1: 초기 로드 → 1페이지 요청 ─────────────────────────────

        private void Case1_InitialLoad()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new PagedResponse<RankingDto>
            {
                Items = new List<RankingDto> { new() { Name = "A", Rank = 1 } },
                Page = 1,
                TotalPages = 3,
            });

            var paged = CreatePaginatedModel(fake);
            paged.GoToPage(1);

            Assert.AreEqual(1, fake.ReceivedRequests.Count);
            Assert.AreEqual("/api/ranking?page=1", fake.ReceivedRequests[0].Url);
            Assert.AreEqual(1, paged.CurrentPage.Value);
            Assert.AreEqual(3, paged.TotalPages.Value);
            Assert.AreEqual(1, paged.Items.Value.Count);

            paged.Dispose();
        }

        // ── Case 2: NextButton → 2페이지 요청 ────────────────────────────

        private void Case2_NextButton()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new PagedResponse<RankingDto>
            {
                Items = new List<RankingDto>(),
                Page = 1,
                TotalPages = 3,
            });
            fake.Returns(new PagedResponse<RankingDto>
            {
                Items = new List<RankingDto> { new() { Name = "B", Rank = 2 } },
                Page = 2,
                TotalPages = 3,
            });

            var paged = CreatePaginatedModel(fake);
            paged.GoToPage(1);

            Assert.AreEqual(1, paged.CurrentPage.Value);
            Assert.AreEqual(true, paged.NextButton.Feature<InteractableFeature>().Interactable.Value);

            paged.NextButton.OnNext(Unit.Default);  // 다음 페이지

            Assert.AreEqual(2, paged.CurrentPage.Value);
            Assert.AreEqual(2, fake.ReceivedRequests.Count);
            Assert.AreEqual("/api/ranking?page=2", fake.ReceivedRequests[1].Url);

            paged.Dispose();
        }

        // ── Case 3: PrevButton → 이전 페이지 요청 ────────────────────────

        private void Case3_PrevButton()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new PagedResponse<RankingDto> { Items = new(), Page = 2, TotalPages = 3 });
            fake.Returns(new PagedResponse<RankingDto> { Items = new(), Page = 1, TotalPages = 3 });

            var paged = CreatePaginatedModel(fake);
            paged.GoToPage(2);

            Assert.AreEqual(2, paged.CurrentPage.Value);
            Assert.AreEqual(true, paged.PrevButton.Feature<InteractableFeature>().Interactable.Value);

            paged.PrevButton.OnNext(Unit.Default);

            Assert.AreEqual(1, paged.CurrentPage.Value);
            Assert.AreEqual("/api/ranking?page=1", fake.ReceivedRequests[1].Url);

            paged.Dispose();
        }

        // ── Case 4: 1페이지에서 PrevButton 비활성화 ──────────────────────

        private void Case4_PrevButtonDisabledOnPage1()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new PagedResponse<RankingDto> { Items = new(), Page = 1, TotalPages = 5 });

            var paged = CreatePaginatedModel(fake);
            paged.GoToPage(1);

            Assert.AreEqual(false, paged.PrevButton.Feature<InteractableFeature>().Interactable.Value);
            Assert.AreEqual(true, paged.NextButton.Feature<InteractableFeature>().Interactable.Value);

            paged.Dispose();
        }

        // ── Case 5: 마지막 페이지에서 NextButton 비활성화 ────────────────

        private void Case5_NextButtonDisabledOnLastPage()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new PagedResponse<RankingDto> { Items = new(), Page = 5, TotalPages = 5 });

            var paged = CreatePaginatedModel(fake);
            paged.GoToPage(5);

            Assert.AreEqual(true, paged.PrevButton.Feature<InteractableFeature>().Interactable.Value);
            Assert.AreEqual(false, paged.NextButton.Feature<InteractableFeature>().Interactable.Value);

            paged.Dispose();
        }

        // ── Case 6: Items PropModel → ListComponent 바인딩 가능 ──────────

        private void Case6_ItemsPropModel()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new PagedResponse<RankingDto>
            {
                Items = new List<RankingDto>
                {
                    new() { Name = "Alpha", Rank = 1 },
                    new() { Name = "Beta",  Rank = 2 },
                    new() { Name = "Gamma", Rank = 3 },
                },
                Page = 1,
                TotalPages = 2,
            });

            var paged = CreatePaginatedModel(fake);

            // PropModel<List<IViewModel>>을 외부에서 구독 (ListComponent 시뮬레이션)
            int observedCount = 0;
            paged.Items.Subscribe(items => observedCount = items?.Count ?? 0).AddTo(disposables);

            paged.GoToPage(1);

            Assert.AreEqual(3, observedCount);
            Assert.AreEqual(3, paged.Items.Value.Count);

            // 자식 ViewModel에서 "rank" PropModel 접근 가능
            var firstItem = paged.Items.Value[0];
            var rankModel = firstItem.GetChild<PropModel<int>>("rank");
            Assert.IsNotNull(rankModel);
            Assert.AreEqual(1, rankModel.Value);

            paged.Dispose();
        }
    }
}
