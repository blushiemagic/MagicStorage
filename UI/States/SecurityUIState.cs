using MagicStorage.Common.Players;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.UI.Input;
using MagicStorage.UI.Security;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerousCommonLib.UI;
using SerousCommonLib.UI.Layouts;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public class SecurityUIState : BaseStorageUI {
		public override string DefaultPage => "Networks";

		public override string GetSearchText() => throw new NotImplementedException();

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Networks";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Networks" => new NetworksPage(this),
				_ => throw new ArgumentException("Unknown page: " + page, nameof(page))
			};

		protected override void PostInitializePages() {
			base.PostInitializePages();

			float innerPanelWidth = 600f + EnvironmentGUI.Padding;
			PanelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight;
		}

		public override float GetMinimumResizeHeight() {
			// I'm too lazy to calculate the "actual" mininum height, so let's just use some arbitrary value and call it a day
			return 240f;
		}

		public override void Recalculate() {
			base.Recalculate();

			if (!Main.gameMenu && PanelHeight < GetMinimumResizeHeight()) {
				// Attempt to force the UI layout to one that takes up less vertical space
				if (MagicUI.AttemptForcedLayoutChange(this))
					return;

				MagicUI.CloseUIDueToHeightLimit();
				pendingUIChange = true;
			}
		}

		public class NetworksPage : BaseStorageUIPage {
			public SecurityNetworkSearchNameTextInputBar searchName;
			public UITextPanel<LocalizedText> searchMode;
			private int _searchModeIndex;
			public UITextPanel<LocalizedText> sortMode;
			private int _sortModeIndex;
			public UITextPanel<LocalizedText> createNetwork;
			public UITextPanel<LocalizedText> clientResponses;
			private bool _clientResponsesDirty;

		//	private BaseOrderedLayout _modeLayout;
			private UIElement _modeLayout;
			private bool _modeLayoutDirty;
			
			private NetworkInfoPopup _networkInfoPopup;
			private PasswordRequestPopup _passwordPrompt;
			private UIElement _popupBlocker;
			private UIElement _serverWaitBlocker;
			private bool _serverBlockerEnabled;
			
			private int _needNetworkPopup;
			private const int NETWORK_POPUP_NONE = 0;
			private const int NETWORK_POPUP_CREATE = 1;
			private const int NETWORK_POPUP_UPDATE = 2;
			private const int NETWORK_POPUP_PASSWORD_FOR_CONFIG = 3;
			private const int NETWORK_POPUP_DESTROY = 4;
			private const int NETWORK_POPUP_PASSWORD_FOR_JOIN = 5;
			private const int NETWORK_POPUP_REASSIGNMENT_LOGIC = 6;
			
			private int _pendingNetworkAction;
			private object _pendingNetworkActionData;
			private NetworkActionResult _networkActionResult;
			private NetworkReportCategory _networkActionCategory;
			private const int NETWORK_ACTION_NONE = 0;
			private const int NETWORK_ACTION_CREATION = 1;
			private const int NETWORK_ACTION_PASSWORD_JOIN_CONFIG = 2;
			private const int NETWORK_ACTION_UPDATE = 3;
			private const int NETWORK_ACTION_DESTROY = 4;
			private const int NETWORK_ACTION_PASSWORD_JOIN = 5;
			private const int NETWORK_ACTION_DIRECT_JOIN = 6;
			private const int NETWORK_ACTION_DIRECT_JOIN_CONFIG = 7;
			private const int NETWORK_ACTION_ASSIGNMENT_CHANGE = 8;

		//	private VerticalLayoutList list;
			private NewUIList list;
			private NewUIScrollbar scroll;

			private int _lastKnownNetworkCount;

			private NetworkReport _activeNetwork;

			public NetworksPage(BaseStorageUI parent) : base(parent, "Networks") {
				searchName = new SecurityNetworkSearchNameTextInputBar();
				_modeLayout = new UIElement();
				searchMode = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Security.UI.Modes.Search.All"));
				sortMode = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Security.UI.Modes.Sort.Name"));
				createNetwork = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Security.UI.Create"));
				list = new();
				scroll = new();
				_popupBlocker = new UIElement();
				_serverWaitBlocker = new UIElement();

				OnPageSelected += () => {
					SecuritySystem.OnClientResultUpdated += SetResponse;
					SecuritySystem.OnInformResultToClient += ReceiveClientResult;

					SetNamePromptText(string.Empty);
					SetSearch(SecuritySystem.SEARCHMODE_ALL);
					SetSort(SecuritySystem.SORTMODE_NAME);

					DestroyCurrentPopup();

					_networkInfoPopup = null;
					_passwordPrompt = null;
					_activeNetwork = null;

					_needNetworkPopup = NETWORK_POPUP_NONE;

					_pendingNetworkAction = NETWORK_ACTION_NONE;
					_pendingNetworkActionData = null;
					_networkActionResult = default;
					_networkActionCategory = default;

					_lastKnownNetworkCount = -1;
					NetHelper.RequestSecurityNetworkList();

					SecuritySystem.ResetClientResult();
				};

				OnPageDeselected += () => {
					DestroyCurrentPopup();

					SecuritySystem.OnClientResultUpdated -= SetResponse;
					SecuritySystem.OnInformResultToClient -= ReceiveClientResult;
				};
			}

			private void SetNamePromptText(string text) {
				if (text == string.Empty)
					searchName.State.Clear();
				else
					searchName.State.Set(text);

				SetNameText(text);
			}

			private static void SetNameText(string text) {
				SecuritySystem.searchName = text;
				SecuritySystem.clientListDirty = true;
			}

			private void SetSearch(int index) {
				_searchModeIndex = index;

				string key = index switch {
					SecuritySystem.SEARCHMODE_ALL => "All",
					SecuritySystem.SEARCHMODE_RESTRICTED => "Restricted",
					SecuritySystem.SEARCHMODE_PUBLIC => "Public",
					_ => throw new ArgumentOutOfRangeException(nameof(index))
				};

				searchMode.SetText(Language.GetText("Mods.MagicStorage.Security.UI.Modes.Search." + key));

				SecuritySystem.searchPrivateMode = index;
				SecuritySystem.clientListDirty = true;

				_modeLayoutDirty = true;
			}

			private void SetSort(int index) {
				_sortModeIndex = index;

				int actualSortType = index / 2;

				string key = actualSortType switch {
					SecuritySystem.SORTMODE_NAME => "Name",
					SecuritySystem.SORTMODE_CREATOR => "Creator",
					SecuritySystem.SORTMODE_ID => "ID",
					_ => throw new ArgumentOutOfRangeException(nameof(index))
				};

				bool reverse = false;
				if (index % 2 == 1) {
					key += "Reverse";
					reverse = true;
				}

				sortMode.SetText(Language.GetText("Mods.MagicStorage.Security.UI.Modes.Sort." + key));

				SecuritySystem.searchSortMode = actualSortType;
				SecuritySystem.sortInReverse = reverse;
				SecuritySystem.clientListDirty = true;

				_modeLayoutDirty = true;
			}

			private void ReceiveClientResult(NetworkActionResult response, NetworkReportCategory category) {
				_serverBlockerEnabled = response == NetworkActionResult.NeedsServerApproval;
				_networkActionResult = response;
				_networkActionCategory = category;
			}

			private void SetResponse(LocalizedText text) {
				clientResponses.SetText(text);
				_clientResponsesDirty = true;
			}

			public override void OnInitialize() {
				base.OnInitialize();

				searchName.Width.Set(-16, 1f);
				searchName.OnInputChangedEvent += self => SetNameText(self.State.InputText);
				/*
				searchName.GetLayoutManager().Attributes = new LayoutAttributes()
					.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: 8f))
					.InheritSizeFrom(searchName);
				*/
				searchName.Left.Set(8, 0f);

				Append(searchName);

				/*
				_modeLayout = new HorizontalLayout() {
					Spacing = new LayoutUnit(pixels: 4f)
				};
				_modeLayout.GetLayoutManager().Attributes = new LayoutAttributes()
					.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: 8f))
					.AddConstraint(LayoutConstraintType.TopToBottomOf, searchName, new LayoutUnit(pixels: 4f))
					.WithSize(
						width: new LayoutUnit(-16f, 1f),
						height: LayoutUnit.DynamicSize
					);
				*/
				_modeLayout.Left.Set(8, 0f);
				_modeLayout.Top.Set(searchName.MinHeight.Pixels + 4, 0f);
				_modeLayout.Width.Set(-16, 1f);

				searchMode.OnLeftClick += (evt, e) => SetSearch((_searchModeIndex + 1) % 3);
				searchMode.OnRightClick += (evt, e) => SetSearch(Utility.Repeat(_searchModeIndex - 1, 3));
				/*
				searchMode.GetLayoutManager().Attributes = new LayoutAttributes()
					.InheritSizeFrom(searchMode);
				*/

				sortMode.OnLeftClick += (evt, e) => SetSort((_sortModeIndex + 1) % 6);
				sortMode.OnRightClick += (evt, e) => SetSort(Utility.Repeat(_sortModeIndex - 1, 6));
				/*
				searchMode.GetLayoutManager().Attributes = new LayoutAttributes()
					.InheritSizeFrom(searchMode);
				*/

				/*
				_modeLayout.AddElement(searchMode);
				_modeLayout.AddElement(sortMode);
				*/
				searchMode.Left.Set(4, 0f);
				searchMode.Width.Set(-6, 0.5f);
				_modeLayout.Append(searchMode);

				sortMode.SetRightAlignment(4);
				sortMode.Width.Set(-6, 0.5f);
				_modeLayout.Append(sortMode);

				_modeLayout.Height.Set(Math.Max(searchMode.MinHeight.Pixels, sortMode.MinHeight.Pixels), 0f);

				Append(_modeLayout);

				// Button needs to be initialized here so that the list can reference it
				createNetwork.OnLeftClick += (evt, e) => _needNetworkPopup = NETWORK_POPUP_CREATE;
				createNetwork.SetBasicHoverColorChangeEvents();

				UIElement listWrapper = new();
				listWrapper.Left.Set(8, 0f);
				listWrapper.Top.Set(_modeLayout.Top.Pixels + _modeLayout.Height.Pixels + 4, 0f);
				listWrapper.Width.Set(-16, 1f);

				list.SetPadding(0);
				/*
				list.GetLayoutManager().Attributes = new LayoutAttributes()
					.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: 8f))
					.AddConstraint(LayoutConstraintType.RightToRightOf, null, new LayoutUnit(pixels: 8f))
					.AddConstraint(LayoutConstraintType.TopToBottomOf, _modeLayout, new LayoutUnit(pixels: 4f))
					.AddConstraint(LayoutConstraintType.BottomToTopOf, createNetwork, new LayoutUnit(pixels: 4f))
					.WithSize(
						width: LayoutUnit.Fill,
						height: LayoutUnit.Fill
					);
				*/
			//	list.Left.Set(8, 0f);
			//	list.Top.Set(_modeLayout.Top.Pixels + _modeLayout.Height.Pixels + 4, 0f);
			//	list.Width.Set(-16, 1f);
				list.Width.Set(-28, 1f);
				list.Height.Set(-20, 1f);
				list.Left.Set(0, 0f);
				list.Top.Set(0, 0f);

				scroll.Height.Set(-30, 1f);
				scroll.Left.Set(-20, 1f);
				scroll.Top.Set(10, 0f);

				list.SetScrollbar(scroll);
				// NOTE: The NewUIScrollBar should NOT be appended to the NewUIList directly, since its children think that it has a (practically) infinite height
			//	list.Append(scroll);
				list.Padding = 6;
			//	Append(list);
				listWrapper.Append(list);
				listWrapper.Append(scroll);
				Append(listWrapper);

				// Grab the reference from the parent and reuse it here
				clientResponses = parentUI.clientResponses;
				clientResponses.Remove();
				Append(clientResponses);

				/*
				createNetwork.GetLayoutManager().Attributes = new LayoutAttributes()
					.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: 8f))
					.AddConstraint(LayoutConstraintType.RightToRightOf, null, new LayoutUnit(pixels: 8f))
					.AddConstraint(LayoutConstraintType.BottomToTopOf, clientResponses, new LayoutUnit(pixels: 8f))
					.InheritSizeFrom(createNetwork);
				*/
				createNetwork.Left.Set(8, 0f);
				createNetwork.SetBottomAlignment(-clientResponses.Top.Pixels + clientResponses.MinHeight.Pixels + 8);
				createNetwork.Width.Set(-16f, 1f);

				Append(createNetwork);

			//	list.Height.Set(-_modeLayout.Height.Pixels - 4 - createNetwork.MinHeight.Pixels - 4 - clientResponses.MinHeight.Pixels - 16, 1f);
				listWrapper.Height.Set(-listWrapper.Top.Pixels -_modeLayout.Height.Pixels - 4 + createNetwork.Top.Pixels - 8, 1f);

				_popupBlocker.Width.Set(0, 1f);
				_popupBlocker.Height.Set(0, 1f);

				_serverWaitBlocker.Width.Set(0, 1f);
				_serverWaitBlocker.Height.Set(0, 1f);
			}

			private void CreateNewNetworkPopup() {
				DestroyCurrentPopup();

				SecuritySystem.ResetClientResult();

				if (_networkInfoPopup is null) {
					NetHelper.Report(false, "  Creating new information popup...");

					_networkInfoPopup = new NetworkInfoPopup() {
						View = default,
						IsUpdating = false
					};
					// Popup sets its layout attributes in its constructor
				} else {
					NetHelper.Report(false, "  Modifying existing information popup for network creation...");

					_networkInfoPopup.View = default;
					_networkInfoPopup.IsUpdating = false;
					_networkInfoPopup.ResetEvents();
				}

				_networkInfoPopup.OnConfirm += CheckNetworkCreation;
				_networkInfoPopup.OnCancel += CancelNetworkInfoPopup;

				_popupBlocker.Append(_networkInfoPopup);

				_popupBlocker.Activate();
				_networkInfoPopup.ClearInputs();

				Append(_popupBlocker);
			}

			private void CheckNetworkCreation(NetworkInfoPopup self) {
				NetworkActionResult result = SecuritySystem.CreateNetwork(self.NetworkName, self.Password, self.Restricted);

				NetHelper.Report(true, $"Network creation result: {result}");

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Creation);

				_pendingNetworkAction = NETWORK_ACTION_CREATION;
			}

			private void CheckNetworkCreation_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				if (_networkActionResult.IsSuccess()) {
					_needNetworkPopup = NETWORK_POPUP_DESTROY;
					SoundEngine.PlaySound(SoundID.MenuClose);
				}
			}

			private void CancelNetworkInfoPopup(NetworkInfoPopup self) {
				NetHelper.Report(true, "Closing information popup...");

				_needNetworkPopup = NETWORK_POPUP_DESTROY;
				SoundEngine.PlaySound(SoundID.MenuClose);

				_pendingNetworkAction = NETWORK_ACTION_NONE;
				_pendingNetworkActionData = null;
			}

			private void CreatePasswordRequestForConfigPopup(SecuritySystem.NetworkView view) {
				DestroyCurrentPopup();

				SecuritySystem.ResetClientResult();

				if (_passwordPrompt is null) {
					NetHelper.Report(false, "  Creating new password request popup...");

					_passwordPrompt = new PasswordRequestPopup(view);
					// Popup sets its layout attributes in its constructor
				} else {
					NetHelper.Report(false, "  Modifying existing password request popup for network configuration...");

					_passwordPrompt.UpdateView(view);
					_passwordPrompt.ResetEvents();
				}

				_passwordPrompt.OnPasswordEntered += CheckNetworkAccessForConfig;
				_passwordPrompt.OnCancel += CancelPasswordRequestPopup;

				_popupBlocker.Append(_passwordPrompt);
				
				_popupBlocker.Activate();
				_passwordPrompt.ClearInputs();

				Append(_popupBlocker);
			}

			private void CancelPasswordRequestPopup(PasswordRequestPopup self) {
				NetHelper.Report(true, "Closing password request popup...");

				_needNetworkPopup = NETWORK_POPUP_DESTROY;
				SoundEngine.PlaySound(SoundID.MenuClose);

				_pendingNetworkAction = NETWORK_ACTION_NONE;
				_pendingNetworkActionData = null;
			}

			private void CheckNetworkAccessForConfig(string enteredPassword) {
				NetworkActionResult result = SecuritySystem.JoinNetwork(_activeNetwork.View.id, enteredPassword);

				NetHelper.Report(true, $"Network join for configuration access result: {result}");

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Access);

				_pendingNetworkActionData = enteredPassword;

				_pendingNetworkAction = NETWORK_ACTION_PASSWORD_JOIN_CONFIG;
			}

			private void CheckNetworkAccessForConfig_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				SecuritySystem.HandleNetworkAccessibilityOnJoin(_networkActionResult, Main.LocalPlayer, _activeNetwork.View.id, (string)_pendingNetworkActionData);

				if (_networkActionResult.IsSuccess())
					_needNetworkPopup = NETWORK_POPUP_UPDATE;
			}

			private void CreateUpdateNetworkPopup(SecuritySystem.NetworkView view) {
				DestroyCurrentPopup();

				SecuritySystem.ResetClientResult();

				if (_networkInfoPopup is null) {
					NetHelper.Report(false, "  Creating new information popup...");

					_networkInfoPopup = new NetworkInfoPopup() {
						View = view,
						IsUpdating = true
					};
					// Popup sets its layout attributes in its constructor
				} else {
					NetHelper.Report(false, "  Modifying existing information popup for network update...");

					_networkInfoPopup.View = view;
					_networkInfoPopup.IsUpdating = true;
					_networkInfoPopup.ResetEvents();
				}
				
				_networkInfoPopup.OnConfirm += CheckNetworkUpdate;
				_networkInfoPopup.OnDelete += CheckNetworkDestroy;
				_networkInfoPopup.OnCancel += CancelNetworkInfoPopup;

				_popupBlocker.Append(_networkInfoPopup);

				_popupBlocker.Activate();
				_networkInfoPopup.ClearInputs();

				Append(_popupBlocker);
			}

			private void CheckNetworkUpdate(NetworkInfoPopup self) {
				NetworkActionResult result = SecuritySystem.ModifyNetwork(_activeNetwork.View.id, self.NetworkName, self.Password, self.Restricted);

				NetHelper.Report(true, $"Network update result: {result}");

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Modification);

				_pendingNetworkAction = NETWORK_ACTION_UPDATE;
			}

			private void CheckNetworkUpdate_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				if (_networkActionResult.IsSuccess()) {
					_needNetworkPopup = NETWORK_POPUP_DESTROY;
					SoundEngine.PlaySound(SoundID.MenuClose);

					SecuritySystem.clientListDirty = true;
				}
			}

			private void CheckNetworkDestroy(NetworkInfoPopup self) {
				NetworkActionResult result = SecuritySystem.RemoveNetwork(_activeNetwork.View.id, self.Password);

				NetHelper.Report(true, $"Network deletion result: {result}");

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Removal);

				_pendingNetworkAction = NETWORK_ACTION_DESTROY;
			}

			private void CheckNetworkDestroy_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				SecuritySystem.HandleNetworkAccessibilityOnRemoval(_networkActionResult, Main.LocalPlayer, _activeNetwork.View.id);

				if (_networkActionResult.IsSuccess()) {
					_needNetworkPopup = NETWORK_POPUP_DESTROY;
					SoundEngine.PlaySound(SoundID.MenuClose);

					SecuritySystem.clientListDirty = true;
				}
			}

			private void CreatePasswordRequestForJoinPopup(SecuritySystem.NetworkView view) {
				DestroyCurrentPopup();

				SecuritySystem.ResetClientResult();

				if (_passwordPrompt is null) {
					NetHelper.Report(false, "  Creating new password request popup...");

					_passwordPrompt = new PasswordRequestPopup(view);
					// Popup sets its layout attributes in its constructor
				} else {
					NetHelper.Report(false, "  Modifying existing password request popup for network join...");

					_passwordPrompt.UpdateView(view);
					_passwordPrompt.ResetEvents();
				}
				
				_passwordPrompt.OnPasswordEntered += CheckNetworkAccessForJoin;
				_passwordPrompt.OnCancel += CancelPasswordRequestPopup;

				_popupBlocker.Append(_passwordPrompt);

				_popupBlocker.Activate();
				_passwordPrompt.ClearInputs();

				Append(_popupBlocker);
			}

			private void CheckNetworkAccessForJoin(string enteredPassword) {
				// Password empty?  Set it to null instead
				if (string.IsNullOrEmpty(enteredPassword))
					enteredPassword = null;

				// First, attempt to give the player access to the network
				NetworkActionResult result = SecuritySystem.JoinNetwork(_activeNetwork.View.id, enteredPassword);

				NetHelper.Report(true, $"Network join result: {result}");

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Join);

				_pendingNetworkActionData = enteredPassword;

				_pendingNetworkAction = NETWORK_ACTION_PASSWORD_JOIN;
			}

			private void CheckNetworkAccessForJoin_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				SecuritySystem.HandleNetworkAccessibilityOnJoin(_networkActionResult, Main.LocalPlayer, _activeNetwork.View.id, (string)_pendingNetworkActionData);

				if (_networkActionResult.IsSuccess())
					_needNetworkPopup = NETWORK_POPUP_REASSIGNMENT_LOGIC;
			}

			private void CheckNetworkReassignment(int networkID) {
				NetworkActionResult result = AssignCurrentStorageToNetwork(networkID);

				NetHelper.Report(true, $"Network reassignment result: {result}");

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.NetworkChange);

				_pendingNetworkAction = NETWORK_ACTION_ASSIGNMENT_CHANGE;
			}

			private void CheckNetworkReassignment_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				_needNetworkPopup = NETWORK_POPUP_DESTROY;

				SecuritySystem.clientListDirty = true;
			}

			private static NetworkActionResult AssignCurrentStorageToNetwork(int networkID) {
				if (Main.LocalPlayer.GetModPlayer<StoragePlayer>().GetStorageHeart() is TEStorageHeart heart)
					return SecuritySystem.AssignNetwork(heart, networkID);
				
				Main.NewText("Attempted to assign a network to a nonexistent storage system.  How did you get here?", color: Color.Red);
				return NetworkActionResult.EntityNotFound;
			}

			private void DestroyCurrentPopup() {
				_networkInfoPopup?.RemoveAndDeactivate();
				_passwordPrompt?.RemoveAndDeactivate();
				_popupBlocker.RemoveAndDeactivate();
			}

			public override void Update(GameTime gameTime) {
				if (SecuritySystem.clientListDirty || _lastKnownNetworkCount != SecuritySystem.NetworkCount) {
					SecuritySystem.clientListDirty = false;
					_lastKnownNetworkCount = SecuritySystem.NetworkCount;
					RefreshList();

					// If a network was being updated, reset the popup
					if (_networkInfoPopup?.Parent is not null && _networkInfoPopup.IsUpdating && _activeNetwork is not null)
						CreateUpdateNetworkPopup(_activeNetwork.View);
				}

				CheckForNetworkActions();
				CheckForPopups();

				if (_clientResponsesDirty) {
					_clientResponsesDirty = false;
					clientResponses.Recalculate();
				}

				if (_serverBlockerEnabled) {
					if (_serverWaitBlocker.Parent is null) {
						_serverWaitBlocker.Activate();
						_popupBlocker.Append(_serverWaitBlocker);
					}
				} else {
					if (_serverWaitBlocker.Parent is not null)
						_serverWaitBlocker.RemoveAndDeactivate();
				}

				base.Update(gameTime);

				Player player = Main.LocalPlayer;

				if (Main.mouseX > parentUI.PanelLeft && Main.mouseX < parentUI.PanelRight && Main.mouseY > parentUI.PanelTop && Main.mouseY < parentUI.PanelBottom) {
					player.mouseInterface = true;
					player.cursorItemIconEnabled = false;
					InterfaceHelper.HideItemIconCache();
				}
			}

			private void CheckForNetworkActions() {
				if (_pendingNetworkAction != NETWORK_ACTION_NONE && !_serverBlockerEnabled) {
					try {
						switch (_pendingNetworkAction) {
							case NETWORK_ACTION_CREATION:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_CREATION)}\"");
								CheckNetworkCreation_Result();
								break;
							case NETWORK_ACTION_PASSWORD_JOIN_CONFIG:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_PASSWORD_JOIN_CONFIG)}\"");
								CheckNetworkAccessForConfig_Result();
								break;
							case NETWORK_ACTION_UPDATE:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_UPDATE)}\"");
								CheckNetworkUpdate_Result();
								break;
							case NETWORK_ACTION_DESTROY:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_DESTROY)}\"");
								CheckNetworkDestroy_Result();
								break;
							case NETWORK_ACTION_PASSWORD_JOIN:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_PASSWORD_JOIN)}\"");
								CheckNetworkAccessForJoin_Result();
								break;
							case NETWORK_ACTION_DIRECT_JOIN:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_DIRECT_JOIN)}\"");
								AttemptNetworkAccessForJoin_Result();
								break;
							case NETWORK_ACTION_DIRECT_JOIN_CONFIG:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_DIRECT_JOIN_CONFIG)}\"");
								AttemptNetworkAccessForConfig_Result();
								break;
							case NETWORK_ACTION_ASSIGNMENT_CHANGE:
								NetHelper.Report(true, $"Executing security action \"{nameof(NETWORK_ACTION_ASSIGNMENT_CHANGE)}\"");
								CheckNetworkReassignment_Result();
								break;
						}
					} catch (Exception ex) {
						Main.NewTextMultiline(ex.ToString(), c: Color.Red);
					} finally {
						_pendingNetworkAction = NETWORK_ACTION_NONE;
						_pendingNetworkActionData = null;
						_networkActionResult = default;
						_networkActionCategory = default;
					}
				}
			}

			private void CheckForPopups() {
				if (_needNetworkPopup != NETWORK_POPUP_NONE) {
					try {
						switch (_needNetworkPopup) {
							case NETWORK_POPUP_CREATE:
								NetHelper.Report(true, $"Executing security popup request \"{nameof(NETWORK_POPUP_CREATE)}\"");
								CreateNewNetworkPopup();
								SoundEngine.PlaySound(SoundID.MenuOpen);
								break;
							case NETWORK_POPUP_UPDATE:
								NetHelper.Report(true, $"Executing security popup request \"{nameof(NETWORK_POPUP_UPDATE)}\"");
								CreateUpdateNetworkPopup(_activeNetwork.View);
								SoundEngine.PlaySound(SoundID.MenuOpen);
								break;
							case NETWORK_POPUP_PASSWORD_FOR_CONFIG:
								NetHelper.Report(true, $"Executing security popup request \"{nameof(NETWORK_POPUP_PASSWORD_FOR_CONFIG)}\"");
								CreatePasswordRequestForConfigPopup(_activeNetwork.View);
								SoundEngine.PlaySound(SoundID.MenuOpen);
								break;
							case NETWORK_POPUP_DESTROY:
								NetHelper.Report(true, $"Executing security popup request \"{nameof(NETWORK_POPUP_DESTROY)}\"");
								DestroyCurrentPopup();
								_activeNetwork = null;
								break;
							case NETWORK_POPUP_PASSWORD_FOR_JOIN:
								NetHelper.Report(true, $"Executing security popup request \"{nameof(NETWORK_POPUP_PASSWORD_FOR_JOIN)}\"");
								CreatePasswordRequestForJoinPopup(_activeNetwork.View);
								SoundEngine.PlaySound(SoundID.MenuOpen);
								break;
							case NETWORK_POPUP_REASSIGNMENT_LOGIC:
								NetHelper.Report(true, $"Executing security popup request \"{nameof(NETWORK_POPUP_REASSIGNMENT_LOGIC)}\"");
								CheckNetworkReassignment(_activeNetwork.View.id);
								break;
						}
					} catch (Exception ex) {
						Main.NewTextMultiline(ex.ToString(), c: Color.Red);
					} finally {
						_needNetworkPopup = NETWORK_POPUP_NONE;
					}
				}
			}

			private void RefreshList() {
				foreach (var element in list)
					element.RemoveAndDeactivate();

				list.Clear();

				int assignedNetwork = StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart ? heart.assignedNetwork : -1;

				foreach (var view in SecuritySystem.EnumerateNetworksWithSettings()) {
					NetworkReport report = new(view);

					if (view.id == assignedNetwork)
						report.DefaultBorderColorOverride = Color.ForestGreen;

					report.OnSetNetwork += AttemptNetworkAccessForJoin;
					report.OnConfigClick += AttemptNetworkAccessForConfig;

					report.Activate();

					list.Add(report);
				}

				list.Recalculate();
			}

			private void AttemptNetworkAccessForJoin(NetworkReport self) {
				if (_activeNetwork is not null)
					return;

				_activeNetwork = self;

				if (self.View.restricted && !SecuritySystem.CanPlayerAccessImmediately(Main.LocalPlayer, self.View.id)) {
					// Password must be provided to access this network
					_needNetworkPopup = NETWORK_POPUP_PASSWORD_FOR_JOIN;
				} else {
					// No password required or the player already has accessed it
					NetworkActionResult attempt = SecuritySystem.AccessNetwork(self.View.id);

					NetHelper.Report(true, $"Network direct access result: {attempt}");

					SecuritySystem.ReportNetworkResult(attempt, NetworkReportCategory.NetworkChange);

					_pendingNetworkAction = NETWORK_ACTION_DIRECT_JOIN;
				}
			}

			private void AttemptNetworkAccessForJoin_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				SecuritySystem.HandleNetworkAccessibilityOnAccess(_networkActionResult, Main.LocalPlayer, _activeNetwork.View.id);

				if (_networkActionResult.IsSuccess())
					_needNetworkPopup = NETWORK_POPUP_REASSIGNMENT_LOGIC;
			}

			private void AttemptNetworkAccessForConfig(NetworkReport self) {
				if (_activeNetwork is not null)
					return;

				_activeNetwork = self;

				if (self.View.restricted && !Main.LocalPlayer.GetModPlayer<SecurityPlayer>().HasJoinedNetwork(self.View.id)) {
					// Password must be provided to access this network
					// Unlike joining the network, the password MUST be known by the local client (Operators have access to the network, but may not know the password for it)
					_needNetworkPopup = NETWORK_POPUP_PASSWORD_FOR_CONFIG;
				} else {
					// No password required, just access the network
					NetworkActionResult attempt = SecuritySystem.AccessNetwork(self.View.id);

					NetHelper.Report(true, $"Network direct access for configuration result: {attempt}");

					SecuritySystem.ReportNetworkResult(attempt, NetworkReportCategory.Access);

					_pendingNetworkAction = NETWORK_ACTION_DIRECT_JOIN_CONFIG;
				}
			}

			private void AttemptNetworkAccessForConfig_Result() {
				NetHelper.Report(false, $"  Result: {_networkActionResult}");

				if (_networkActionResult.IsSuccess())
					_needNetworkPopup = NETWORK_POPUP_UPDATE;
			}

			protected override void DrawChildren(SpriteBatch spriteBatch) {
				if (_modeLayoutDirty) {
					_modeLayoutDirty = false;
					_modeLayout.Recalculate();
				}

				base.DrawChildren(spriteBatch);
			}
		}
	}
}
