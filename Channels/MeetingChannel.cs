﻿using System;
using Discord;
using Discord.WebSocket;

namespace OkawariBot.Channels
{
	public class MeetingChannel
	{
		/// <summary>
		/// 勉強会中のボイスチャンネル
		/// </summary>
		public IVoiceChannel VoiceChannel { get; set; }
		/// <summary>
		/// 勉強会中のテキストチャンネル
		/// </summary>
		public IMessageChannel MessageChannel { get; set; }
		/// <summary>
		/// インフォメーションのメッセージ
		/// </summary>
		public IUserMessage? InformationMessage { get; set; }
		/// <summary>
		/// 現在
		/// </summary>
		public string CurrentTopic { get; set; } = "未設定";
		private int _nowJoinerCount { get; set; } = -1;
		private System.Timers.Timer _eventTimer = new System.Timers.Timer(1000);
		/// <summary>
		/// トピックを開始する
		/// </summary>
		public void Start()
		{
			this._eventTimer.Elapsed += async (sender, e) => await this.EventLoop(sender,e);
			this._eventTimer.Start();
		}
		public void Stop()
		{
			this._eventTimer.Dispose();
			this._eventTimer = new System.Timers.Timer(1000);
		}
		public async Task EventLoop(object sender, System.Timers.ElapsedEventArgs e)
		{
			int joinerCount = 0;
			IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> joiners = this.VoiceChannel.GetUsersAsync();
			await foreach (var joiner in joiners)
			{
				joinerCount += joiner.Count;
			}
			if (joinerCount > _nowJoinerCount && _nowJoinerCount != -1)
			{
				await this.SendInformation();
			}
			this._nowJoinerCount = joinerCount;
		}
		/// <summary>
		/// 進行に関する情報を送信する
		/// </summary>
		public async Task SendInformation(EmbedFieldBuilder? additionalInfoField = null)
		{
			await this.TryDeleteInfoMessage();
			EmbedBuilder builder = this.GetInfoEmbed();
			if (additionalInfoField is not null)
			{
				builder.AddField(additionalInfoField);
			}
			this.InformationMessage = await this.MessageChannel.SendMessageAsync(embed: builder.Build());
		}
		public async Task<bool> TryDeleteInfoMessage()
		{
			if (this.InformationMessage is not null)
			{
				await this.MessageChannel.DeleteMessageAsync(this.InformationMessage.Id);
				this.InformationMessage = null;
				return true;
			}
			return false;
		} 
		private EmbedBuilder GetInfoEmbed()
		{
			var builder = new EmbedBuilder()
			{
				Title = "インフォメーション",
				Description = $"現在の進行に関する情報です。",
				Color = Color.LightOrange,
			};
			builder.AddField(new EmbedFieldBuilder()
			{
				Name = "【トピック】",
				Value = $"{this.CurrentTopic}\n",
			});
			return builder;
		}
		/// <summary>
		/// 参加者全員をメンションしたメッセージの文字列を返す
		/// </summary>
		/// <returns>タイマー開始した人が参加しているボイスチャンネルに参加している人を全員メンションしたメッセージの文字列</returns>
		public async Task<string> GetVoiceChannelUsersMentionMessage()
		{
			string mentionMessage = "";
			List<ulong> ids = await this.GetVoiceChannelUserIds();
			foreach (var id in ids)
			{
				mentionMessage += $"<@!{id}>";
			}
			return mentionMessage;
		}
		/// <summary>
		/// ボイスチャンネルに参加しているユーザのIdのリストを取得する。
		/// </summary>
		/// <returns>ボイスチャンネルに参加しているユーザのIdのリスト</returns>
		public async Task<List<ulong>> GetVoiceChannelUserIds()
		{
			var userList = new List<ulong>();
			var usersCollection = this.VoiceChannel.GetUsersAsync();
			await foreach (var users in usersCollection)
			{
				foreach (var user in users)
				{
					userList.Add(user.Id);
				}
			}
			return userList;
		}
	}
}
